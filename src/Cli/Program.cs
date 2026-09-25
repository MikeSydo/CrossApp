using System.Text;
using Core.Domain;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("=== Сценарій 1: успіх ===");

Order order = Order.Create("O-001", "C-001");

order.AddLine("P-001", "Клавіатура", 1200m, 2);
order.AddLine("P-002", "Миша", 600m, 1);

Console.WriteLine($"Замовлення: {order.Id}");
Console.WriteLine($"Клієнт: {order.CustomerId}");

foreach (OrderLine line in order.Lines)
    Console.WriteLine($"  {line.Name}: {line.Quantity} × {line.Price} = {line.Subtotal} грн");

Console.WriteLine($"Загальна сума: {order.Total} грн");

order.Confirm();
Console.WriteLine($"Підтверджено: {order.IsConfirmed}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

TryDo("створення замовлення з порожнім id", () => { Order draft = Order.Create("", "C-002"); });
TryDo("створення замовлення з порожнім customerId", () => { Order draft = Order.Create("O-002", ""); });
TryDo("додавання рядка після підтвердження", () => order.AddLine("P-004", "Монітор", 8000m, 1));
TryDo("підтвердження порожнього замовлення",
    () =>
    {
        Order emptyOrder = Order.Create("O-003", "C-003");
        emptyOrder.Confirm();
    });
TryDo("повторне підтвердження", () => order.Confirm());

Console.WriteLine();
TryDo("додавання порожнього id товару", () => order.AddLine("", "Монітор", 8000m, 1));
TryDo("додавання товару з порожнім name", () => order.AddLine("P-003", "", 500m, 3));
TryDo("товар з від'ємною ціною",
    () =>
    {
        Order draft = Order.Create("O-002", "C-002");
        draft.AddLine("P-004", "Навушники", -900m, 1);
    });
TryDo("нульова кількість товару",
    () =>
    {
        Order draft = Order.Create("O-002", "C-002");
        draft.AddLine("P-004", "Навушники", 900m, 0);
    });

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