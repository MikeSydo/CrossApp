using Core.Abstractions;

namespace Core.Services;

//Сервіс замовлень із сумісним найменуванням для методички.
public class CatalogService(IOrderStore store, INotificationSink? sink = null) : OrderService(store, sink)
{
}
