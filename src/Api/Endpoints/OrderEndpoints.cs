using System.Security.Cryptography;
using Api.Contracts;
using Api.Validation;
using Core.Abstractions;
using Core.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrders(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapGet("/", (IOrderStore store, int? page, int? pageSize, string? search) =>
        {
            var p = page.GetValueOrDefault(1);
            var ps = pageSize.GetValueOrDefault(20);
            if (p < 1) p = 1;
            if (ps < 1) ps = 20;
            if (ps > 100) ps = 100;

            var all = store.List();
            IEnumerable<Order> query = all;

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o =>
                    o.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    o.CustomerId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    o.Lines.Any(l => l.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                     l.ProductId.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            var list = query.ToList();
            var total = list.Count;
            var items = list
                .Skip((p - 1) * ps)
                .Take(ps)
                .Select(o => o.ToApiDto())
                .ToList();

            return TypedResults.Ok(new PagedResult<OrderDto>(items, total));
        });

        group.MapGet("/{id}", Results<Ok<OrderDto>, NotFound>
            (string id, IOrderStore store) =>
            store.GetById(id) is { } found
                ? TypedResults.Ok(found.ToApiDto())
                : TypedResults.NotFound());

        group.MapGet("/{id}/total", Results<Ok<OrderTotalResponse>, NotFound>
            (string id, IOrderStore store) =>
            store.GetById(id) is { } found
                ? TypedResults.Ok(new OrderTotalResponse(found.Id, found.Total))
                : TypedResults.NotFound());

        group.MapPost("/", Results<Created<OrderDto>, ValidationProblem>
            (SaveOrderRequest request, IOrderStore store) =>
        {
            var id = $"O-{Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant()}";
            var created = Order.Create(id, request.EffectiveCustomer, request.CreatedAt);

            foreach (var line in request.Lines!)
            {
                created.AddLine(line.ProductId!, line.Name!, line.Price, line.Quantity);
            }

            store.Add(created);
            return TypedResults.Created($"/api/orders/{created.Id}", created.ToApiDto());
        }).AddEndpointFilter<ValidationFilter<SaveOrderRequest>>();

        group.MapPut("/{id}", Results<Ok<OrderDto>, NotFound, ValidationProblem>
            (string id, SaveOrderRequest request, IOrderStore store) =>
        {
            if (store.GetById(id) is null)
                return TypedResults.NotFound();

            var replacement = Order.Create(id, request.EffectiveCustomer, request.CreatedAt);
            foreach (var line in request.Lines!)
            {
                replacement.AddLine(line.ProductId!, line.Name!, line.Price, line.Quantity);
            }

            store.Update(replacement);
            return TypedResults.Ok(replacement.ToApiDto());
        }).AddEndpointFilter<ValidationFilter<SaveOrderRequest>>();

        group.MapDelete("/{id}", Results<NoContent, NotFound>
            (string id, IOrderStore store) =>
            store.Remove(id)
                ? TypedResults.NoContent()
                : TypedResults.NotFound());
    }
}
