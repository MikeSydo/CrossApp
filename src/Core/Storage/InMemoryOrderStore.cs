using System.Collections.Concurrent;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryOrderStore : IOrderStore 
{
    private readonly ConcurrentDictionary<string, Order> _items;

    public InMemoryOrderStore(IEnumerable<Order>? seed = null)
    {
        _items = new ConcurrentDictionary<string, Order>(
            (seed ?? []).ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }
    
    public IReadOnlyList<Order> List() => _items.Values.ToList();

    public Order? GetById(string id) => _items.GetValueOrDefault(id);

    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.TryAdd(item.Id, item))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");
    }

    public void Update(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items[item.Id] = item;
    }

    public bool Remove(string id) => _items.TryRemove(id, out _);
}
