using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private IReadOnlyList<Order>? _cachedOrders;

    public IReadOnlyList<Order> List()
    {
        _cachedOrders ??= _inner.List().ToList().AsReadOnly();
        return _cachedOrders;
    }
    public Order? GetById(string id) => List().FirstOrDefault(order => 
        string.Equals(order.Id, id, StringComparison.OrdinalIgnoreCase));
    public void Add(Order item)
    {
        _inner.Add(item);
        _cachedOrders = null;
    }
    public void Update(Order item)
    {
        _inner.Update(item);
        _cachedOrders = null;
    }
    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);

        if (removed) 
            _cachedOrders = null;

        return removed;
    }
}