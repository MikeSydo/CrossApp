using System.Globalization;
using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class OrderService(IOrderStore store, INotificationSink sink)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    public event EventHandler<OrderChangedEventArgs>? Changed;
    
    public Order CreateOrder(string customerId)
    {
        Order order = Order.Create(Guid.NewGuid().ToString("N")[..8], customerId);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.OrderCreated);
        return order;
    }
    public void AddLine(string orderId, string productId, string name, decimal price, int quantity)
    {
        Order order = GetOrThrow(orderId);
        order.AddLine(productId,  name, price, quantity);
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded);
    }
    public void ConfirmOrder(string orderId)
    {
        Order order = GetOrThrow(orderId);
        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderConfirmed);
    }
    public IReadOnlyList<Order> All() => _store.List();
    public Order? Find(string id) => _store.GetById(id);
    public IReadOnlyList<Order> Search(Func<Order, bool> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return _store.List().Where(parameter).ToList();
    }
    
    public int ImportFromLines(IEnumerable<string> lines, Action<string>? onSkipped = null)
    {
        ArgumentNullException.ThrowIfNull(lines);
        int imported = 0;

        foreach (var line in lines)
        {
            try
            {
                // format: customerId;productId;name;price;quantity
                var parts = line.Split(';');
                if (parts.Length != 5)
                    throw new FormatException("Очікується 5 полів через ';'.");

                var customerId = parts[0].Trim();
                var productId = parts[1].Trim();
                var name = parts[2].Trim();
                var price = decimal.Parse(parts[3], CultureInfo.InvariantCulture);
                var quantity = int.Parse(parts[4], CultureInfo.InvariantCulture);

                var order = CreateOrder(customerId);
                AddLine(order.Id, productId, name, price, quantity);
                imported++;
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException)
            {
                onSkipped?.Invoke($"Пропущено рядок: '{line}' ({ex.Message})");
            }
        }

        return imported;
    }
    
    private Order GetOrThrow(string id) => 
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає замовлення з id={id}.");

    private void OnChanged(Order o, OrderChangeKind kind)
    {
        var e = new OrderChangedEventArgs(o.Id, kind, o.Lines.Count, o.Total);
        _sink.WriteLine($"{e.At:HH:mm:ss} {e.Kind,-14} {e.Id} рядків: {e.LinesCount,2} -> сума {e.Total}");
        Changed?.Invoke(this, e);
    }
}