using Core.Domain;
using Core.Dto;

namespace Core;

public static class SampleData
{
    public static IEnumerable<CustomerDto> Customers() =>
    [
        new("C-001", "Олександр Коваленко", "oleksandr@example.com"),
        new("C-002", "Марія Шевченко", "maria@example.com"),
        new("C-003", "Іван Мельник", "ivan@example.com"),
        new("C-004", "Олена Ткаченко", "olena@example.com"),
        new("C-005", "Дмитро Кравченко", "dmytro@example.com"),
        new("C-006", "Анна Бондаренко", "anna@example.com")
    ];

    public static IEnumerable<Order> Orders()
    {
        var products = new (string Id, string Name, decimal Price)[]
        {
            ("P-001", "Клавіатура", 850m),
            ("P-002", "Миша", 420m),
            ("P-003", "Монітор", 6500m),
            ("P-004", "USB-кабель", 180m),
            ("P-005", "Навушники", 1200m)
        };
        
        for (int i = 1; i <= 18; i++)
        {
            Order order = Order.Create(
                id: $"O-{i:000}",
                customerId: $"C-{(i - 1) % 6 + 1:000}");
            
            // Забезпечуємо як малі замовлення (для report-low), так і великі
            if (i == 1)
            {
                order.AddLine(products[3].Id, products[3].Name, products[3].Price, 1); // USB-кабель (180 грн)
            }
            else if (i == 2)
            {
                order.AddLine(products[1].Id, products[1].Name, products[1].Price, 1); // Миша (420 грн)
            }
            else if (i == 3)
            {
                order.AddLine(products[0].Id, products[0].Name, products[0].Price, 1); // Клавіатура (850 грн)
            }
            else
            {
                int lineCount = (i - 1) % 5 + 1;
                for (int j = 0; j < lineCount; j++)
                {
                    var product = products[j];
                    order.AddLine(product.Id, product.Name, product.Price, quantity: j + 1);
                }
            }

            // Розподіляємо статуси для показового GroupBy (Draft, Confirmed, Cancelled)
            if (i % 3 == 0)
            {
                order.Confirm();
            }
            else if (i % 5 == 0)
            {
                order.Cancel();
            }

            yield return order;
        }
    }

    /// <summary>
    /// Генератор великої кількості замовлень для бенчмарку Stopwatch (додаткове завдання 4).
    /// </summary>
    public static IEnumerable<Order> GenerateOrders(int count)
    {
        var products = new (string Id, string Name, decimal Price)[]
        {
            ("P-001", "Клавіатура", 850m),
            ("P-002", "Миша", 420m),
            ("P-003", "Монітор", 6500m),
            ("P-004", "USB-кабель", 180m),
            ("P-005", "Навушники", 1200m)
        };

        for (int i = 1; i <= count; i++)
        {
            var order = Order.Create($"GEN-{i:000000}", $"C-{(i % 50) + 1:000}");
            var product = products[i % products.Length];
            order.AddLine(product.Id, product.Name, product.Price, (i % 5) + 1);

            if (i % 2 == 0)
                order.Confirm();

            yield return order;
        }
    }
}