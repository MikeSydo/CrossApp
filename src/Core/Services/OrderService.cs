using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class OrderService(IOrderStore store)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Order CreateOrder(string customerId)
    {
        Order order = Order.Create(Guid.NewGuid().ToString("N")[..8], customerId);
        _store.Add(order);
        return order;
    }
    public void AddLine(string orderId, string productId, string name, decimal price, int quantity)
    {
        Order order = GetOrThrow(orderId);
        order.AddLine(productId,  name, price, quantity);
        _store.Update(order);
    }
    public void ConfirmOrder(string orderId)
    {
        Order order = GetOrThrow(orderId);
        order.Confirm();
        _store.Update(order);
    }
    public IReadOnlyList<Order> All() => _store.List();
    public Order? Find(string id) => _store.GetById(id);
    public IReadOnlyList<Order> Search(Func<Order, bool> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return _store.List().Where(parameter).ToList();
    }
    private Order GetOrThrow(string id) => 
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає замовлення з id={id}.");
}