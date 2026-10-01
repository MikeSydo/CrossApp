using System.Text;
using Cli;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = Encoding.UTF8;

bool useFile = args.Contains("--file");
string dataPath = Path.Combine("data", "orders.json");
IOrderStore store = useFile
    ? new FileOrderStore(dataPath)
    : new InMemoryOrderStore(SampleData.Orders());
var service = new OrderService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");               
Order created = service.CreateOrder("C-016");
service.AddLine(created.Id, "P-0", "Монітор", 1200, 2);

foreach (var o in service.All())
{
    Console.WriteLine($" {o.Id} {o.CustomerId,-10}");

    foreach (var l in o.Lines)
    {
        Console.WriteLine($"\t{l.ProductId} {l.Name}: {l.Price} * {l.Quantity} = {l.Subtotal}");
    }
}

Console.WriteLine("\nFound order: " + service.Find("O-010")?.Id);

Console.WriteLine("\n=== Обробка помилок ===");
TryDo("Порожній customerId", () => service.CreateOrder(""));
TryDo("Невідомий orderId", () => service.AddLine("1111", "P-0", "Монітор", 1200, 2));
TryDo("Порожній productId", () => service.AddLine("O-001", "", "Монітор", 1200, 2));
service.ConfirmOrder(created.Id);
TryDo("Повторне підтвердження замовлення", () => service.ConfirmOrder(created.Id));

Console.WriteLine("\n=== Пошук за делегатом ===");
IReadOnlyList<Order> found = service.Search(order => order.Id.StartsWith("O-00", StringComparison.OrdinalIgnoreCase));
foreach (var order in found)
    Console.WriteLine(order.Id);

Console.WriteLine("\n=== Створення через фабрику ===");
var store1 = StoreFactory.Create(args);
var service1 = new OrderService(store);
Console.WriteLine($"Сховище: {store1.GetType().Name}");
Console.WriteLine($"Сервіс: {service1.GetType().Name}");

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