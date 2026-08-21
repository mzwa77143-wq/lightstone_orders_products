using OrdersAndInventory.Domain.Exceptions;

namespace OrdersAndInventory.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    // EF Core constructor
    private Product() { }

    public static Product Create(string sku, string name, decimal price, int stock, DateTime? createdAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainValidationException("Product SKU cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Product name cannot be null or empty.");

        if (price < 0)
            throw new DomainValidationException("Product price cannot be negative.");

        if (stock < 0)
            throw new DomainValidationException("Product stock cannot be negative.");

        var now = createdAtUtc ?? DateTime.UtcNow;

        return new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero),
            Stock = stock,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void AdjustStock(int delta, DateTime? updatedAtUtc = null)
    {
        if (Stock + delta < 0)
        {
            throw new InsufficientStockException(Sku, Math.Abs(delta), Stock);
        }

        Stock += delta;
        UpdatedAtUtc = updatedAtUtc ?? DateTime.UtcNow;
    }

    public void DeductStock(int quantity, DateTime? updatedAtUtc = null)
    {
        if (quantity <= 0)
            throw new DomainValidationException("Quantity to deduct must be greater than zero.");

        if (Stock < quantity)
            throw new InsufficientStockException(Sku, quantity, Stock);

        Stock -= quantity;
        UpdatedAtUtc = updatedAtUtc ?? DateTime.UtcNow;
    }

    public void UpdateDetails(string name, decimal price, DateTime? updatedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Product name cannot be null or empty.");

        if (price < 0)
            throw new DomainValidationException("Product price cannot be negative.");

        Name = name.Trim();
        Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        UpdatedAtUtc = updatedAtUtc ?? DateTime.UtcNow;
    }
}
