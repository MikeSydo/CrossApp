using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<OrderRow> Orders => Set<OrderRow>();
    public DbSet<OrderLineRow> OrderLines => Set<OrderLineRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<OrderRow>();
        order.HasKey(o => o.Id);
        order.Property(o => o.Id).HasMaxLength(36);
        order.Property(o => o.CustomerId).IsRequired().HasMaxLength(50);
        order.Property(o => o.CreatedAt).IsRequired();
        order.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

        order.HasMany(o => o.Lines)
            .WithOne(l => l.Order)
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderLineRow>(line =>
        {
            line.HasKey(l => l.Id);
            line.Property(l => l.OrderId).IsRequired().HasMaxLength(36);
            line.Property(l => l.ProductId).IsRequired().HasMaxLength(50);
            line.Property(l => l.Name).IsRequired().HasMaxLength(200);
            line.Property(l => l.Price).HasPrecision(18, 2);
            line.Property(l => l.Quantity).IsRequired();
            line.HasIndex(l => new { l.OrderId, l.ProductId }).IsUnique();
        });
    }
}
