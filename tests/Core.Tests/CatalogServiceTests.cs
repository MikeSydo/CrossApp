using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public sealed class CatalogServiceTests
{
    [Fact] //Інваріант: відмова і незмінний стан (DomainException)
    public void AddLine_WhenOrderAlreadyConfirmed_ThrowsDomainException_AndKeepsState()
    {
        var store = new InMemoryOrderStore();
        var service = new CatalogService(store);
        var order = service.CreateOrder("C-001");
        service.AddLine(order.Id, "P-1", "Кабель", 100m, 1);
        service.ConfirmOrder(order.Id);

        var countBefore = store.GetById(order.Id)!.Lines.Count;
        var totalBefore = store.GetById(order.Id)!.Total;

        var ex = Assert.Throws<DomainException>(() =>
            service.AddLine(order.Id, "P-2", "Миша", 250m, 1));

        Assert.Contains("вже підтверджено", ex.Message, StringComparison.OrdinalIgnoreCase);

        var orderAfter = store.GetById(order.Id)!;
        Assert.Equal(countBefore, orderAfter.Lines.Count);
        Assert.Equal(totalBefore, orderAfter.Total);
    }

    [Fact] //Варіант «Замовлення»: тест на суму замовлення після додавання рядків
    public void AddLine_IncreasesOrderTotal_Correctly()
    {
        var store = new InMemoryOrderStore();
        var service = new CatalogService(store);
        var order = service.CreateOrder("C-001");

        service.AddLine(order.Id, "P-100", "Флешка", 300m, 2); //600 грн
        service.AddLine(order.Id, "P-200", "Клавіатура", 1200m, 1); //1200 грн

        var updatedOrder = store.GetById(order.Id)!;
        Assert.Equal(2, updatedOrder.Lines.Count);
        Assert.Equal(1800m, updatedOrder.Total);
    }

    [Fact] //Подія
    public void ConfirmOrder_RaisesChangedExactlyOnce()
    {
        var service = new CatalogService(new InMemoryOrderStore());
        var order = service.CreateOrder("C-001");
        service.AddLine(order.Id, "P-1", "Кабель", 100m, 1);

        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.ConfirmOrder(order.Id);

        Assert.Single(events);
        Assert.Equal(OrderChangeKind.OrderConfirmed, events[0].Kind);
        Assert.Equal(order.Id, events[0].Id);
        Assert.Equal(100m, events[0].Total);
    }
}
