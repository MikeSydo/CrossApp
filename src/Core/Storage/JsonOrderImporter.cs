using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public class JsonOrderImporter : ICatalogImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        List<OrderDto>? dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<OrderDto>>(stream, Options, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            throw new DomainException($"Помилка у форматі JSON файлу {Path.GetFileName(path)}: {ex.Message}", ex);
        }

        if (dtos is null)
        {
            throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");
        }

        var result = new List<Order>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                result.Add(Order.FromDto(dtos[i]));
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message}", ex);
            }
        }

        return result;
    }

    public async IAsyncEnumerable<Order> ReadAsync(
        string path,
        [EnumeratorCancellation] CancellationToken ct = default,
        IProgress<int>? progress = null)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        int totalCount = await GetArrayCountAsync(path, ct);

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        IAsyncEnumerable<OrderDto?> dtos;
        try
        {
            dtos = JsonSerializer.DeserializeAsyncEnumerable<OrderDto>(stream, Options, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            throw new DomainException($"Помилка у форматі JSON файлу {Path.GetFileName(path)}: {ex.Message}", ex);
        }

        int index = 0;
        await foreach (var dto in dtos.WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            if (dto is null) continue;

            index++;
            Order order;
            try
            {
                order = Order.FromDto(dto);
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                throw new DomainException($"Запис {index}: {ex.Message}", ex);
            }

            // Вивід відсотка на ходу
            if (totalCount > 0)
            {
                int percent = (int)(index * 100.0 / totalCount);
                progress?.Report(percent);
            }

            yield return order;
        }

        progress?.Report(100);
    }

    private static async Task<int> GetArrayCountAsync(string path, CancellationToken ct)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 
                4096, true);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return doc.RootElement.GetArrayLength();
            }
        }
        catch
        {
            // Якщо формат не масив, розрахунок відсотків пропускається
        }

        return 0;
    }
}
