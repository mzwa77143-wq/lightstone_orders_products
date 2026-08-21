using FluentAssertions;
using OrdersAndInventory.Domain.Entities;
using OrdersAndInventory.Domain.Exceptions;

namespace OrdersAndInventory.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInstantiateProduct()
    {
        // Arrange & Act
        var product = Product.Create("SKU-100", "Wireless Mouse", 29.99m, 50);

        // Assert
        product.Id.Should().NotBeEmpty();
        product.Sku.Should().Be("SKU-100");
        product.Name.Should().Be("Wireless Mouse");
        product.Price.Should().Be(29.99m);
        product.Stock.Should().Be(50);
        product.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        product.UpdatedAtUtc.Should().Be(product.CreatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidSku_ShouldThrowDomainValidationException(string? invalidSku)
    {
        // Act
        var act = () => Product.Create(invalidSku!, "Test Product", 10.0m, 10);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*SKU*");
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldThrowDomainValidationException()
    {
        // Act
        var act = () => Product.Create("SKU-101", "Test Product", -5.0m, 10);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*price*");
    }

    [Fact]
    public void Create_WithNegativeStock_ShouldThrowDomainValidationException()
    {
        // Act
        var act = () => Product.Create("SKU-101", "Test Product", 10.0m, -1);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*stock*");
    }

    [Fact]
    public void AdjustStock_PositiveDelta_ShouldIncreaseStock()
    {
        // Arrange
        var product = Product.Create("SKU-100", "Item", 10m, 20);

        // Act
        product.AdjustStock(15);

        // Assert
        product.Stock.Should().Be(35);
    }

    [Fact]
    public void AdjustStock_NegativeDeltaWithinAvailable_ShouldDecreaseStock()
    {
        // Arrange
        var product = Product.Create("SKU-100", "Item", 10m, 20);

        // Act
        product.AdjustStock(-5);

        // Assert
        product.Stock.Should().Be(15);
    }

    [Fact]
    public void AdjustStock_NegativeDeltaExceedingStock_ShouldThrowInsufficientStockException()
    {
        // Arrange
        var product = Product.Create("SKU-100", "Item", 10m, 10);

        // Act
        var act = () => product.AdjustStock(-15);

        // Assert
        act.Should().Throw<InsufficientStockException>()
            .Where(ex => ex.Sku == "SKU-100" && ex.RequestedQuantity == 15 && ex.AvailableStock == 10);
    }

    [Fact]
    public void DeductStock_ValidQuantity_ShouldDecreaseStock()
    {
        // Arrange
        var product = Product.Create("SKU-100", "Item", 10m, 25);

        // Act
        product.DeductStock(10);

        // Assert
        product.Stock.Should().Be(15);
    }

    [Fact]
    public void DeductStock_ExceedingStock_ShouldThrowInsufficientStockException()
    {
        // Arrange
        var product = Product.Create("SKU-100", "Item", 10m, 5);

        // Act
        var act = () => product.DeductStock(10);

        // Assert
        act.Should().Throw<InsufficientStockException>();
    }
}
