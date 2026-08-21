using FluentAssertions;
using OrdersAndInventory.Application.DTOs;
using OrdersAndInventory.Application.Validators;

namespace OrdersAndInventory.UnitTests.Application.Validators;

public class ValidatorTests
{
    private readonly CreateProductRequestValidator _createProductValidator = new();
    private readonly AdjustStockRequestValidator _adjustStockValidator = new();
    private readonly SubmitOrderRequestValidator _submitOrderValidator = new();

    [Fact]
    public void CreateProductRequestValidator_ValidRequest_ShouldPassValidation()
    {
        var request = new CreateProductRequest("VALID-SKU_1", "Sample Item", 49.99m, 100);
        var result = _createProductValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateProductRequestValidator_NegativeStock_ShouldFailValidation()
    {
        var request = new CreateProductRequest("VALID-SKU", "Sample Item", 49.99m, -1);
        var result = _createProductValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Stock");
    }

    [Fact]
    public void AdjustStockRequestValidator_ZeroDelta_ShouldFailValidation()
    {
        var request = new AdjustStockRequest(0);
        var result = _adjustStockValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Delta");
    }

    [Fact]
    public void SubmitOrderRequestValidator_EmptyItems_ShouldFailValidation()
    {
        var request = new SubmitOrderRequest("EXT-123", DateTime.UtcNow, new List<SubmitOrderItemRequest>());
        var result = _submitOrderValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void SubmitOrderRequestValidator_NegativePriceItem_ShouldFailValidation()
    {
        var request = new SubmitOrderRequest("EXT-123", DateTime.UtcNow, new List<SubmitOrderItemRequest>
        {
            new SubmitOrderItemRequest("SKU-1", 1, -10m)
        });
        var result = _submitOrderValidator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
