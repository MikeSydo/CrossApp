using System.Text;
using Core;
using Core.Abstractions;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = Encoding.UTF8;

var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
INotificationSink sink = new FileSink(logPath);

var dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");
if (!File.Exists(dataPath))
{
    dataPath = Path.Combine("data", "orders.json");
}

int fileArgIndex = Array.IndexOf(args, "--file");
if (fileArgIndex >= 0 && fileArgIndex + 1 < args.Length && !args[fileArgIndex + 1].StartsWith("--"))
{
    dataPath = args[fileArgIndex + 1];
}
else
{
    var explicitJson = args.FirstOrDefault(a => a.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
    if (explicitJson is not null)
    {
        dataPath = explicitJson;
    }
}

// Токен користувача (перехоплення Ctrl+C)
using var userCts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    // ReSharper disable once AccessToDisposedClosure
    userCts.Cancel();
};

// Токен таймауту
using var timeoutCts = new CancellationTokenSource();
if (args.Contains("--timeout"))
{
    int timeoutIndex = Array.IndexOf(args, "--timeout");
    if (timeoutIndex >= 0 && timeoutIndex + 1 < args.Length && double.TryParse(args[timeoutIndex + 1], out var seconds))
    {
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(seconds));
    }
    else
    {
        // Демонстрація таймауту за замовчуванням
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(1));
    }
}

// Додаткове завдання 2: зв'язані токени через CreateLinkedTokenSource
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);

// Додаткове завдання 3: IProgress<int> для малювання відсотків у консолі
var progress = new Progress<int>(percent =>
{
    Console.Write($"\rІмпорт: {percent}% ");
});

var service = new OrderService(new InMemoryOrderStore(), sink);

try
{
    IOrderImporter importer = new JsonCatalogImporter();
    int added;

    if (args.Contains("--stream"))
    {
        // Додаткове завдання 1: потоковий імпорт через IAsyncEnumerable
        added = await service.ImportStreamAsync(importer, dataPath, linkedCts.Token, progress);
    }
    else
    {
        // Стандартний асинхронний імпорт
        added = await service.ImportAsync(importer, dataPath, linkedCts.Token, progress);
    }

    Console.WriteLine();
    Console.WriteLine($"Імпортовано записів: {added}");
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.WriteLine("Імпорт скасовано.");
}
catch (DomainException ex)
{
    Console.WriteLine();
    Console.WriteLine($"Дані відхилено: {ex.Message}");
}
catch (FileNotFoundException)
{
    Console.WriteLine();
    Console.WriteLine($"Файл не знайдено: {dataPath}");
}