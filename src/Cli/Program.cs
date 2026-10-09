using System.Text;
using Cli;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Services;

Console.OutputEncoding = Encoding.UTF8;

IOrderStore store = StoreFactory.Create(args);
INotificationSink fileSink = new FileSink(Path.Combine("logs", "app.log"));
var service = new OrderService(store, fileSink);
Console.WriteLine($"Сховище: {store.GetType().Name}");

var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

void OnOrderChanged(object? sender, OrderChangedEventArgs e)
{
    string line = $"{e.At:HH:mm:ss} {e.Kind,-14} {e.Id} рядків: {e.LinesCount,2} -> сума {e.Total}";
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

int changes = 0;
service.Changed += OnOrderChanged;
service.Changed += (_, _) => changes++;

Console.WriteLine("\n=== Успішні операції (події) ===");
Order created = service.CreateOrder("C-016");
service.AddLine(created.Id, "P-0", "Монітор", 1200, 2);
service.AddLine(created.Id, "P-1", "Клавіатура", 800, 1);
service.ConfirmOrder(created.Id);
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine("\n=== Сценарії відмови (подій НЕ має бути) ===");
int before = changes;
TryDo("Порожній customerId", () => service.CreateOrder(""));
TryDo("Невідомий orderId", () => service.AddLine("1111", "P-0", "Монітор", 1200, 2));
TryDo("Порожній productId", () => service.AddLine(created.Id, "", "Монітор", 1200, 2));
TryDo("Від'ємна кількість", () => service.AddLine(created.Id, "P-0", "Монітор", 1200, -1));
TryDo("Повторне підтвердження замовлення", () => service.ConfirmOrder(created.Id));
Console.WriteLine($"Нових подій після відмов: {changes - before}");

Console.WriteLine("\n=== Відписка (-=) ===");
service.Changed -= OnOrderChanged;
Order silent = service.CreateOrder("C-017");
service.AddLine(silent.Id, "P-2", "Миша", 400, 3);
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine("\n=== Пошук за делегатом (Func) ===");
IReadOnlyList<Order> found = service.Search(o => o.Total > 10000);
foreach (var o in found)
    Console.WriteLine($" {o.Id} {o.CustomerId,-10} сума {o.Total}");

Console.WriteLine($"\nЛог: {logPath}");

var skipped = new List<string>();
int imported = service.ImportFromLines([
    "C-100;P-0;Монітор;1200;2",
    "C-101;P-1;Клавіатура;800;1",
    "поганий;рядок;з;малою;кількістю;полів",
    "C-102;P-2;Миша;400;3"
], skipped.Add);

Console.WriteLine($"Імпортовано: {imported}");
Console.WriteLine($"Пропущено: {skipped.Count}");
foreach (var s in skipped)
    Console.WriteLine(s);

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"{title}: виняток НЕ спрацював - перевір інваріант!");
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"{title}: {ex.GetType().Name} - {ex.Message}");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"{title}: {ex.GetType().Name} - {ex.Message}");
    }
}