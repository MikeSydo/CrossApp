using System.Text.Json.Serialization;
using Core.Domain;

namespace Api.Contracts;

public sealed record OrderLineDto(
    string ProductId,
    string Name,
    decimal Price,
    int Quantity,
    decimal Subtotal);

public sealed record OrderDto(
    string Id,
    string Customer,
    string CustomerId,
    DateTime CreatedAt,
    IReadOnlyList<OrderLineDto> Lines,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    OrderStatus Status,
    decimal Total);

public sealed record OrderTotalResponse(
    string Id,
    decimal Total);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total);

public static class OrderMappingExtensions
{
    public static OrderDto ToApiDto(this Order order)
    {
        var lines = order.Lines.Select(l => new OrderLineDto(
            l.ProductId,
            l.Name,
            l.Price,
            l.Quantity,
            l.Subtotal
        )).ToList();

        return new OrderDto(
            order.Id,
            order.Customer,
            order.CustomerId,
            order.CreatedAt,
            lines,
            order.Status,
            order.Total
        );
    }
}
