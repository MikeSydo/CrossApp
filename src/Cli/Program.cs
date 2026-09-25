using System.Text;
using Core.Domain;
using Core.Dto;
using Core.Import;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("=== Сценарій 1: успіх ===");

Order order1 = Order.Create("O-001", "C-001");

order1.AddLine("P-001", "Клавіатура", 1200m, 2);
order1.AddLine("P-002", "Миша", 600m, 1);

Console.WriteLine($"Замовлення: {order1.Id}");
Console.WriteLine($"Клієнт: {order1.CustomerId}");

foreach (OrderLine line in order1.Lines)
    Console.WriteLine($"  {line.Name}: {line.Quantity} × {line.Price} = {line.Subtotal} грн");

Console.WriteLine($"Загальна сума: {order1.Total} грн");

order1.Confirm();
Console.WriteLine($"Підтверджено: {order1.Status}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

TryDo("створення замовлення з порожнім id", () => { Order.Create("", "C-002"); });
TryDo("створення замовлення з порожнім customerId", () => { Order.Create("O-002", ""); });
TryDo("додавання рядка після підтвердження", () => order1.AddLine("P-004", "Монітор", 8000m, 1));
TryDo("підтвердження порожнього замовлення",
    () =>
    {
        Order emptyOrder = Order.Create("O-003", "C-003");
        emptyOrder.Confirm();
    });
TryDo("повторне підтвердження", order1.Confirm);

Console.WriteLine();
TryDo("додавання порожнього id товару", () => order1.AddLine("", "Монітор", 8000m, 1));
TryDo("додавання товару з порожнім name", () => order1.AddLine("P-003", "", 500m, 3));
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

Console.WriteLine();
Console.WriteLine("=== Перетворення імпорту на замовлення ===");

var input = new ImportResult<OrderDto>(
    new List<OrderDto>
    {
        new(
            "O-010",
            "C-001",
            new List<OrderLineDto>
            {
                new("P-001", "Клавіатура", 1200m, 2)
            },
            OrderStatus.Cancelled),

        new(
            "O-011",
            "C-002",
            new List<OrderLineDto>
            {
                new("P-002", "Миша", 600m, 0)
            }),

        new(
            "O-012",
            "C-003",
            new List<OrderLineDto>
            {
                new("P-003", "Монітор", 8000m, 1)
            },
            OrderStatus.Confirmed)
    },
    new List<string>
    {
        "рядок 8: не вдалося розібрати запис"
    });

ImportResult<Order> converted = OrderImporter.Convert(input);

Console.WriteLine($"Створено замовлень: {converted.Items.Count}");

foreach (Order importedOrder in converted.Items)
{
    Console.WriteLine(
        $"{importedOrder.Id}: " +
        $"{importedOrder.Lines.Count} рядків, " +
        $"{importedOrder.Total} грн, " +
        $"Статус: {importedOrder.Status}");
}

foreach (string error in converted.Errors)
{
    Console.WriteLine($"! {error}");
}