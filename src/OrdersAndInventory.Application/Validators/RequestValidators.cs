using FluentValidation;
using OrdersAndInventory.Application.DTOs;

namespace OrdersAndInventory.Application.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("SKU can only contain alphanumeric characters, underscores, and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative.");
    }
}

public class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.Delta)
            .NotEqual(0).WithMessage("Adjustment delta cannot be zero.");
    }
}

public class SubmitOrderRequestValidator : AbstractValidator<SubmitOrderRequest>
{
    public SubmitOrderRequestValidator()
    {
        RuleFor(x => x.ExternalOrderId)
            .NotEmpty().WithMessage("ExternalOrderId is required.")
            .MaximumLength(100).WithMessage("ExternalOrderId cannot exceed 100 characters.");

        RuleFor(x => x.PlacedAtUtc)
            .NotEmpty().WithMessage("PlacedAtUtc is required.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(5)).WithMessage("PlacedAtUtc cannot be in the distant future.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.");

        RuleForEach(x => x.Items).SetValidator(new SubmitOrderItemRequestValidator());
    }
}

public class SubmitOrderItemRequestValidator : AbstractValidator<SubmitOrderItemRequest>
{
    public SubmitOrderItemRequestValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("Item SKU is required.")
            .MaximumLength(50).WithMessage("Item SKU cannot exceed 50 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");
    }
}
