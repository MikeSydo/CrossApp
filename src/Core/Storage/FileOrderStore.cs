using System.Globalization;
using System.Text;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileOrderStore : IOrderStore, IDisposable
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path;
    private readonly string _format;
    private bool _loaded;
    private bool _disposed;
    private string? _temporaryPath;

    public FileOrderStore(string path, string format = "json")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
        _format = NormalizeFormat(format);
    }

    private void EnsureLoaded()
    {
        EnsureNotDisposed();
        if (_loaded)
            return;

        if (File.Exists(_path))
        {
            var dtos = _format == "json"
                ? DeserializeJson(File.ReadAllText(_path))
                : DeserializeCsv(File.ReadAllText(_path));

            for (var index = 0; index < dtos.Count; index++)
            {
                try
                {
                    var order = Order.FromDto(dtos[index]);
                    _cache[order.Id] = order;
                }
                catch (Exception ex) when (ex is ArgumentException or DomainException)
                {
                    throw new DomainException($"Запис {index + 1}: {ex.Message}", ex);
                }
            }
        }

        _loaded = true;
    }

    private void Flush()
    {
        EnsureNotDisposed();
        var directory = Path.GetDirectoryName(_path);
        if (directory is null)
            throw new InvalidOperationException("Не вдалося визначити каталог сховища.");

        Directory.CreateDirectory(directory);
        var temporaryPath = _path + ".tmp";
        _temporaryPath = temporaryPath;

        try
        {
            var content = _format == "json"
                ? JsonSerializer.Serialize(
                    _cache.Values.Select(order => order.ToDto()).ToList(), Options)
                : SerializeCsv();
            File.WriteAllText(temporaryPath, content, new UTF8Encoding(false));
            File.Move(temporaryPath, _path, overwrite: true);
            _temporaryPath = null;
        }
        finally
        {
            if (_temporaryPath is not null)
            {
                DeleteIfExists(_temporaryPath);
                _temporaryPath = null;
            }
        }
    }

    public IReadOnlyList<Order> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public void EnsureExists()
    {
        EnsureNotDisposed();
        if (File.Exists(_path))
            return;

        _loaded = true;
        Flush();
    }

    public Order? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }
    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (!_cache.TryAdd(item.Id, item))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");
        Flush();
    }
    public void Update(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        _cache[item.Id] = item;
        Flush();
    }

    public int ReplaceAll(IEnumerable<Order> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var replacement = items.ToList();
        var replacementCache = new Dictionary<string, Order>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var item in replacement)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (!replacementCache.TryAdd(item.Id, item))
                throw new InvalidOperationException($"Запис з id={item.Id} дублюється.");
        }

        _cache.Clear();
        foreach (var pair in replacementCache)
            _cache.Add(pair.Key, pair.Value);

        _loaded = true;
        Flush();
        return replacement.Count;
    }
    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_temporaryPath is not null)
        {
            DeleteIfExists(_temporaryPath);
            _temporaryPath = null;
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static string NormalizeFormat(string format) =>
        format.Trim().ToLowerInvariant() switch
        {
            "json" => "json",
            "csv" => "csv",
            _ => throw new ArgumentException(
                "Storage:Format має бути json або csv.", nameof(format))
        };

    private static List<OrderDto> DeserializeJson(string content) =>
        JsonSerializer.Deserialize<List<OrderDto>>(content, Options) ?? [];

    private string SerializeCsv()
    {
        var builder = new StringBuilder(
            "orderId,customerId,status,productId,name,price,quantity\r\n");

        foreach (var order in _cache.Values)
        {
            var dto = order.ToDto();
            foreach (var line in dto.Lines)
            {
                builder.Append(EscapeCsv(dto.Id)).Append(',')
                    .Append(EscapeCsv(dto.CustomerId)).Append(',')
                    .Append(dto.Status).Append(',')
                    .Append(EscapeCsv(line.ProductId)).Append(',')
                    .Append(EscapeCsv(line.Name)).Append(',')
                    .Append(line.Price.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(line.Quantity).Append("\r\n");
            }
        }

        return builder.ToString();
    }

    private static List<OrderDto> DeserializeCsv(string content)
    {
        using var reader = new StringReader(content);
        var header = reader.ReadLine();
        if (header is null)
            return [];

        ValidateCsvHeader(header);
        var orders = new Dictionary<string, CsvOrder>(StringComparer.OrdinalIgnoreCase);
        var lineNumber = 1;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (!string.IsNullOrWhiteSpace(line))
                AddCsvRecord(orders, ParseCsvRecord(line, lineNumber));
        }

        return orders.Values
            .Select(order => new OrderDto(
                order.Id, order.CustomerId, order.Lines, order.Status))
            .ToList();
    }

    private static void ValidateCsvHeader(string header)
    {
        var expectedColumns = new[]
        {
            "orderId", "customerId", "status", "productId",
            "name", "price", "quantity"
        };

        if (!ParseCsvLine(header).SequenceEqual(
                expectedColumns, StringComparer.OrdinalIgnoreCase))
        {
            throw new FormatException(
                "CSV має містити заголовок orderId,customerId,status,productId,name,price,quantity.");
        }
    }

    private static CsvRecord ParseCsvRecord(string line, int lineNumber)
    {
        var fields = ParseCsvLine(line);
        if (fields.Count != 7)
            throw new FormatException($"CSV-рядок {lineNumber} має містити 7 полів.");
        if (!Enum.TryParse(fields[2], ignoreCase: true, out OrderStatus status))
            throw new FormatException($"CSV-рядок {lineNumber} містить некоректний статус.");
        if (!decimal.TryParse(
                fields[5], NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            throw new FormatException($"CSV-рядок {lineNumber} містить некоректну ціну.");
        }

        if (!int.TryParse(fields[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity))
            throw new FormatException($"CSV-рядок {lineNumber} містить некоректну кількість.");

        return new CsvRecord(
            fields[0],
            fields[1],
            status,
            new OrderLineDto(fields[3], fields[4], price, quantity));
    }

    private static void AddCsvRecord(
        Dictionary<string, CsvOrder> orders,
        CsvRecord record)
    {
        if (!orders.TryGetValue(record.OrderId, out var order))
        {
            order = new CsvOrder(record.OrderId, record.CustomerId, record.Status);
            orders.Add(record.OrderId, order);
        }
        else if (!string.Equals(order.CustomerId, record.CustomerId, StringComparison.Ordinal) ||
                 order.Status != record.Status)
        {
            throw new FormatException("CSV містить суперечливі дані замовлення.");
        }

        order.Lines.Add(record.Line);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var index = 0;

        while (index < line.Length)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(character);
            }

            index++;
        }

        if (quoted)
            throw new FormatException("CSV містить незакриті лапки.");

        fields.Add(field.ToString());
        return fields;
    }

    private static string EscapeCsv(string value) =>
        value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private sealed class CsvOrder(string id, string customerId, OrderStatus status)
    {
        public string Id { get; } = id;
        public string CustomerId { get; } = customerId;
        public OrderStatus Status { get; } = status;
        public List<OrderLineDto> Lines { get; } = [];
    }

    private sealed record CsvRecord(
        string OrderId,
        string CustomerId,
        OrderStatus Status,
        OrderLineDto Line);
}