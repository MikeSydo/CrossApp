using Core.Domain;

namespace Core;

public static class SampleData
{ 
    public static IEnumerable<Order>? Orders()
    {
        var products = new (string Id, string Name, decimal Price)[]
        {
            ("P-001", "Клавіатура", 850m),
            ("P-002", "Миша", 420m),
            ("P-003", "Монітор", 6500m),
            ("P-004", "USB-кабель", 180m),
            ("P-005", "Навушники", 1200m)
        };
        
        for (int i = 1; i <= 15; i++)
        {
            Order order = Order.Create(
                id: $"O-{i:000}",
                customerId: $"C-{(i - 1) % 5 + 1:000}");
            
            int lineCount = (i - 1) % 5 + 1;
            
            for (int j = 0; j < lineCount; j++)
            {
                var product = products[j];
                order.AddLine(product.Id, product.Name, product.Price, quantity: j + 1);
            }
            yield return order;

        }
    }
}