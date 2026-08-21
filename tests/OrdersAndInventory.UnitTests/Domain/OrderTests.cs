using FluentAssertions;
using OrdersAndInventory.Domain.Entities;
using OrdersAndInventory.Domain.Enums;
using OrdersAndInventory.Domain.Exceptions;

namespace OrdersAndInventory.UnitTests.Domain;

public class OrderTests
{
    [Fact]
    public void Create_WithValidItems_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var placedAt = DateTime.UtcNow;
        var items = new List<(string Sku, int Quantity, decimal UnitPrice)>
        {
            ("SKU-1", 2, 50.00m),
            ("SKU-2", 3, 20.00m)
        };

        // Act
        var order = Order.Create("EXT-ORD-001", placedAt, items);

        // Assert
        order.ExternalOrderId.Should().Be("EXT-ORD-001");
        order.PlacedAtUtc.Should().Be(placedAt);
        order.Status.Should().Be(OrderStatus.Accepted);
        order.Items.Should().HaveCount(2);
        order.TotalAmount.Should().Be(160.00m); // 2*50 + 3*20 = 100 + 60 = 160
    }

    [Fact]
    public void Create_WithEmptyItems_ShouldThrowDomainValidationException()
    {
        // Arrange
        var items = new List<(string Sku, int Quantity, decimal UnitPrice)>();

        // Act
        var act = () => Order.Create("EXT-ORD-002", DateTime.UtcNow, items);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*at least one item*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidExternalOrderId_ShouldThrowDomainValidationException(string? invalidId)
    {
        // Arrange
        var items = new List<(string Sku, int Quantity, decimal UnitPrice)>
        {
            ("SKU-1", 1, 10m)
        };

        // Act
        var act = () => Order.Create(invalidId!, DateTime.UtcNow, items);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*ExternalOrderId*");
    }

    [Fact]
    public void OrderItem_Create_ShouldCalculateTotalPriceCorrectly()
    {
        // Act
        var item = OrderItem.Create(Guid.NewGuid(), "SKU-99", 4, 12.50m);

        // Assert
        item.Quantity.Should().Be(4);
        item.UnitPrice.Should().Be(12.50m);
        item.TotalPrice.Should().Be(50.00m);
    }
}
