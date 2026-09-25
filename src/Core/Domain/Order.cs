using Core.Dto;

namespace Core.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];
    public string Id { get; }
    public string CustomerId { get; }
    public bool IsConfirmed { get; private set; }
    
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
        if (IsConfirmed)
            throw new InvalidOperationException($"Замовлення {Id} вже підтверджене");

        OrderLine line = OrderLine.Create(productId, name, price, quantity);
        _lines.Add(line);
    }

    public void Confirm()
    {
        if (IsConfirmed)
            throw new InvalidOperationException($"Замовлення {Id} вже підтверджене");
        if (_lines.Count == 0)
            throw new InvalidOperationException($"Замовлення {Id} не має рядків");

        IsConfirmed = true;
    }

    public OrderDto ToDto()
    {
        var lines = _lines.Select(line => line.ToDto()).ToList();

        return new OrderDto(Id, CustomerId, lines, IsConfirmed);
    }

    public static Order FromDto(OrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = Create(dto.Id, dto.CustomerId);

        foreach (OrderLineDto line in dto.Lines)
        {
            order.AddLine(line.ProductId, line.Name, line.Price, line.Quantity);
        }

        if (dto.IsConfirmed)
            order.Confirm();

        return order;
    }
}