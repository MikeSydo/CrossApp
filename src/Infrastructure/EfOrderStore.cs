using Core;
using Core.Abstractions;
using Core.Domain;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public sealed class EfOrderStore(AppDbContext db) : IOrderStore
{
    private static Order ToDomain(OrderRow row)
    {
        var order = Order.Create(row.Id, row.CustomerId, row.CreatedAt);
        foreach (var line in row.Lines.OrderBy(l => l.Id))
        {
            order.AddLine(line.ProductId, line.Name, line.Price, line.Quantity);
        }

        switch (row.Status)
        {
            case OrderStatus.Confirmed:
                order.Confirm();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
            case OrderStatus.Draft:
                break;
        }

        return order;
    }

    private static OrderRow ToRow(Order p) => new()
    {
        Id = p.Id,
        CustomerId = p.CustomerId,
        CreatedAt = p.CreatedAt,
        Status = p.Status,
        Lines = p.Lines.Select(line => new OrderLineRow
        {
            OrderId = p.Id,
            ProductId = line.ProductId,
            Name = line.Name,
            Price = line.Price,
            Quantity = line.Quantity
        }).ToList()
    };

    public IReadOnlyList<Order> List() =>
        db.Orders
            .AsNoTracking()
            .Include(x => x.Lines)
            .OrderBy(x => x.Id)
            .AsEnumerable()
            .Select(ToDomain)
            .ToList();

    public Order? GetById(string id) =>
        db.Orders
            .AsNoTracking()
            .Include(x => x.Lines)
            .SingleOrDefault(x => x.Id == id) is { } row
            ? ToDomain(row)
            : null;

    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (db.Orders.Any(x => x.Id == item.Id))
        {
            throw new DomainException($"Замовлення {item.Id} уже існує.");
        }

        db.Orders.Add(ToRow(item));
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            throw new DomainException($"Помилка збереження замовлення {item.Id}: {ex.Message}", ex);
        }
    }

    public void Update(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var row = db.Orders.Include(x => x.Lines).SingleOrDefault(x => x.Id == item.Id);
        if (row is null)
        {
            throw new InvalidOperationException($"Замовлення {item.Id} не знайдено.");
        }

        row.CustomerId = item.CustomerId;
        row.CreatedAt = item.CreatedAt;
        row.Status = item.Status;

        db.OrderLines.RemoveRange(row.Lines);
        row.Lines = item.Lines.Select(line => new OrderLineRow
        {
            OrderId = item.Id,
            ProductId = line.ProductId,
            Name = line.Name,
            Price = line.Price,
            Quantity = line.Quantity
        }).ToList();

        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            throw new DomainException($"Помилка оновлення замовлення {item.Id}: {ex.Message}", ex);
        }
    }

    public bool Remove(string id)
    {
        var row = db.Orders.SingleOrDefault(x => x.Id == id);
        if (row is null) return false;
        db.Orders.Remove(row);
        db.SaveChanges();
        return true;
    }
}
