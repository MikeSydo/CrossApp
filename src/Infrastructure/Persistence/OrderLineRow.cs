namespace Infrastructure.Persistence;

public sealed class OrderLineRow
{
    public int Id { get; set; }
    public string OrderId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public OrderRow Order { get; set; } = null!;
}
