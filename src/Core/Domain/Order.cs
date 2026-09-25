using Core.Dto;

namespace Core.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];
    public string Id { get; }
    public string CustomerId { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(line => line.Subtotal);
    
    private Order(string id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
    }

    public static Order Create(string id, string customerId)
    {
        if (string.IsNullOrWhiteSpace(id)) 
            throw new ArgumentException("Ідентифікатор замовлення не може бути порожнім", nameof(id));
        if (string.IsNullOrWhiteSpace(customerId)) 
            throw new ArgumentException("Ідентифікатор клієнта не може бути порожнім", nameof(customerId));

        return new Order(id.Trim(), customerId.Trim());
    }

    public void AddLine(string productId, string name, decimal price, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Замовлення {Id} вже підтверджене");

        OrderLine line = OrderLine.Create(productId, name, price, quantity);
        
        if (_lines.Any(existing => string.Equals(existing.ProductId, line.ProductId, 
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Товар {line.ProductId} уже є в замовленні {Id}");
        }
        
        _lines.Add(line);
    }

    public void Confirm()
    {
        if (_lines.Count == 0 && Status == OrderStatus.Draft)
            throw new InvalidOperationException($"Замовлення {Id} не має рядків");

        Status = Status switch
        {
            OrderStatus.Draft => OrderStatus.Confirmed,

            OrderStatus.Confirmed => throw new InvalidOperationException($"Замовлення {Id} вже підтверджене"),

            OrderStatus.Cancelled => throw new InvalidOperationException(
                $"Замовлення {Id} скасоване, підтвердити його не можна"),

            _ => throw new InvalidOperationException($"Невідомий стан замовлення {Id}: {Status}")
        };
    }

    public void Cancel()
    {
        Status = Status switch
        {
            OrderStatus.Draft => OrderStatus.Cancelled,

            OrderStatus.Confirmed => throw new InvalidOperationException(
                $"Підтверджене замовлення {Id} не можна скасувати"),

            OrderStatus.Cancelled => throw new InvalidOperationException($"Замовлення {Id} вже скасоване"),

            _ => throw new InvalidOperationException($"Невідомий стан замовлення {Id}: {Status}")
        };
    }

    public OrderDto ToDto()
    {
        var lines = _lines.Select(line => line.ToDto()).ToList();

        return new OrderDto(Id, CustomerId, lines, Status);
    }

    public static Order FromDto(OrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = Create(dto.Id, dto.CustomerId);

        foreach (OrderLineDto line in dto.Lines)
        {
            order.AddLine(line.ProductId, line.Name, line.Price, line.Quantity);
        }

        switch (dto.Status)
        {
            case  OrderStatus.Confirmed:
                order.Confirm();
                break;
            case  OrderStatus.Cancelled:
                order.Cancel();
                break;
            case OrderStatus.Draft:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dto.Status), dto.Status, "Невідомий статус замовлення");
        }

        return order;
    }
}