using System.Reflection;
using System.Text;
using System.Text.Json;
using Core;
using Core.Configuration;
using Core.Storage;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = Encoding.UTF8;

try
{
    var environmentName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT")
        ?? "Development";

    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
        .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
        .AddEnvironmentVariables(prefix: "CROSSAPP_")
        .AddCommandLine(args)
        .Build();

    var options = configuration.Get<AppOptions>() ?? new AppOptions();
    var configuredDataPath = GetOption(args, "--Storage:DataPath")
                             ?? Environment.GetEnvironmentVariable("CROSSAPP_STORAGE__DATAPATH");
    var commandLineDataPath = GetOption(args, "--data-path");
    var explicitDataDirectory = commandLineDataPath ?? configuredDataPath;
    var dataPath = explicitDataDirectory ?? options.Storage.DataPath;

    var storage = new StorageOptions
    {
        DataPath = dataPath,
        FileName = options.Storage.FileName,
        Format = GetOption(args, "--Storage:Format") 
                 ?? options.Storage.Format
    };
    var currency = GetOption(args, "--Orders:Currency")
        ?? GetOption(args, "--orders:currency")
        ?? options.Orders.Currency;
    if (string.IsNullOrWhiteSpace(currency))
        throw new ArgumentException("Orders:Currency не може бути порожнім.");

    var ordersPath = explicitDataDirectory is null
        ? storage.ResolveFullPath(AppContext.BaseDirectory)
        : Path.GetFullPath(Path.Combine(
            explicitDataDirectory, "data", storage.FileName));
    var importPath = GetOption(args, "--import");
    var apiKey = configuration["Integration:ApiKey"];

    Console.WriteLine($"Середовище: {options.EnvironmentName}");
    Console.WriteLine($"Формат: {storage.Format}");
    Console.WriteLine($"Валюта: {currency}");
    Console.WriteLine($"Файл замовлень: {ordersPath}");
    Console.WriteLine(apiKey is null
        ? "ApiKey: відсутній"
        : $"ApiKey: налаштований ({apiKey.Length} символів)");
    using var store = new FileOrderStore(ordersPath, storage.Format);
    store.EnsureExists();
    var exportPath = GetOption(args, "--export");

    if (importPath is not null)
    {
        var imported = ImportOrders(store, importPath);
        Console.WriteLine($"Імпортовано замовлень: {imported}");
    }

    if (exportPath is not null)
    {
        var exported = ExportOrders(store, exportPath);
        Console.WriteLine($"Експортовано замовлень: {exported}");
    }

    var orders = store.List();
    Console.WriteLine($"Замовлень у сховищі: {orders.Count}");

    if (storage.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
    {
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        Console.WriteLine(JsonSerializer.Serialize(
            orders.Select(order => order.ToDto()).ToList(), jsonOptions));
    }
    else
    {
        Console.WriteLine(
            $"OrderId   CustomerId     Status   ProductId  Name                  Price ({currency})  Qty");
        Console.WriteLine(
            "--------  -------------  -------  ---------  --------------------  ------------  ---");

        foreach (var order in orders)
        {
            foreach (var line in order.Lines)
            {
                Console.WriteLine(
                    $"{order.Id,-8}  {order.CustomerId,-13}  {order.Status,-7}  " +
                    $"{line.ProductId,-9}  {line.Name,-20}  {line.Price,12}  {line.Quantity,3}");
            }
        }
    }

    return 0;
}
catch (Exception ex) when (ex is IOException or JsonException or FormatException or
                           ArgumentException or InvalidOperationException or DomainException)
{
    await Console.Error.WriteLineAsync($"Помилка: {ex.Message}");
    return 2;
}

static string? GetOption(string[] args, string name)
{
    var prefix = name + "=";
    var inline = args.FirstOrDefault(argument =>
        argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    if (inline is not null)
        return inline[prefix.Length..];

    var index = Array.FindIndex(args, argument =>
        argument.Equals(name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length && !args[index + 1].StartsWith("--")
        ? args[index + 1]
        : null;
}

static int ImportOrders(FileOrderStore destination, string sourcePath)
{
    var sourceFormat = GetFormatFromPath(sourcePath);
    using var source = new FileOrderStore(sourcePath, sourceFormat);
    var orders = source.List();
    return destination.ReplaceAll(orders);
}

static int ExportOrders(FileOrderStore source, string targetPath)
{
    var targetFormat = GetFormatFromPath(targetPath);
    var targetDirectory = Path.GetDirectoryName(Path.GetFullPath(targetPath));
    if (targetDirectory is not null)
        Directory.CreateDirectory(targetDirectory);

    using var target = new FileOrderStore(targetPath, targetFormat);
    return target.ReplaceAll(source.List());
}

static string GetFormatFromPath(string path)
{
    var extension = Path.GetExtension(path);
    return extension.ToLowerInvariant() switch
    {
        ".json" => "json",
        ".csv" => "csv",
        _ => throw new ArgumentException(
            $"Файл '{path}' має непідтримуване розширення. Використовуйте .json або .csv.")
    };
}
