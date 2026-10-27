using Core.Domain;
using Core.Dto;

namespace Core.Tests;

public static class TestData
{
    public static IReadOnlyList<Order> Orders()
    {
        // 1) Мале замовлення (Draft): Total = 500
        var o1 = Order.Create("O-1", "C-1");
        o1.AddLine("P-1", "USB-кабель", 500m, 1);

        // 2) Середнє замовлення (Confirmed): Total = 2500 (500*2 + 1500*1)
        var o2 = Order.Create("O-2", "C-2");
        o2.AddLine("P-1", "USB-кабель", 500m, 2);
        o2.AddLine("P-2", "Миша", 1500m, 1);
        o2.Confirm();

        // 3) Велике замовлення (Confirmed): Total = 3000 (3000*1)
        var o3 = Order.Create("O-3", "C-1");
        o3.AddLine("P-3", "Клавіатура", 3000m, 1);
        o3.Confirm();

        // 4) Дуже мале замовлення (Cancelled): Total = 200 (100*2)
        var o4 = Order.Create("O-4", "C-3");
        o4.AddLine("P-4", "Килимок", 100m, 2);
        o4.Cancel();

        return [o1, o2, o3, o4];
    }

    public static IReadOnlyList<CustomerDto> Customers() =>
    [
        new("C-1", "Іван Петренко", "ivan@example.com"),
        new("C-2", "Марія Сидоренко", "maria@example.com"),
        new("C-3", "Олексій Коваль", "oleksiy@example.com")
    ];
}
