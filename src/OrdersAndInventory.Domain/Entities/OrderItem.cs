using OrdersAndInventory.Domain.Exceptions;

namespace OrdersAndInventory.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }

    // EF Core constructor
    private OrderItem() { }

    public static OrderItem Create(Guid orderId, string productSku, int quantity, decimal unitPrice)
    {
        if (orderId == Guid.Empty)
            throw new DomainValidationException("OrderId cannot be empty.");

        if (string.IsNullOrWhiteSpace(productSku))
            throw new DomainValidationException("Product SKU cannot be null or empty.");

        if (quantity <= 0)
            throw new DomainValidationException("Quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new DomainValidationException("UnitPrice cannot be negative.");

        var roundedUnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
        var totalPrice = decimal.Round(quantity * roundedUnitPrice, 2, MidpointRounding.AwayFromZero);

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductSku = productSku.Trim().ToUpperInvariant(),
            Quantity = quantity,
            UnitPrice = roundedUnitPrice,
            TotalPrice = totalPrice
        };
    }
}
