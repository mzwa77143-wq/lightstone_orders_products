using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrdersAndInventory.Application.Common.Interfaces;
using OrdersAndInventory.Domain.Entities;

namespace OrdersAndInventory.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public IExecutionStrategy CreateExecutionStrategy()
    {
        return Database.CreateExecutionStrategy();
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    public IDbConnection GetDbConnection()
    {
        return Database.GetDbConnection();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product Configuration
        modelBuilder.Entity<Product>(builder =>
        {
            builder.ToTable("Products", t =>
            {
                t.HasCheckConstraint("CK_Products_Stock_NonNegative", "[Stock] >= 0");
                t.HasCheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
            });

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Sku)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(p => p.Sku)
                .IsUnique()
                .HasDatabaseName("IX_Products_Sku");

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(p => p.Stock)
                .IsRequired();

            builder.Property(p => p.CreatedAtUtc)
                .IsRequired();

            builder.Property(p => p.UpdatedAtUtc)
                .IsRequired();
        });

        // Order Configuration
        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.ExternalOrderId)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(o => o.ExternalOrderId)
                .IsUnique()
                .HasDatabaseName("IX_Orders_ExternalOrderId");

            builder.Property(o => o.PlacedAtUtc)
                .IsRequired();

            builder.HasIndex(o => o.PlacedAtUtc)
                .HasDatabaseName("IX_Orders_PlacedAtUtc");

            builder.Property(o => o.CreatedAtUtc)
                .IsRequired();

            builder.Property(o => o.TotalAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(o => o.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(o => o.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // OrderItem Configuration
        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.ToTable("OrderItems");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.ProductSku)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(i => i.ProductSku)
                .HasDatabaseName("IX_OrderItems_ProductSku");

            builder.HasIndex(i => new { i.OrderId, i.ProductSku })
                .HasDatabaseName("IX_OrderItems_OrderId_ProductSku");

            builder.Property(i => i.Quantity)
                .IsRequired();

            builder.Property(i => i.UnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(i => i.TotalPrice)
                .HasPrecision(18, 2)
                .IsRequired();
        });
    }
}
