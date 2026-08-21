namespace OrdersAndInventory.Domain.Enums;

public enum OrderProcessingOutcome
{
    Accepted = 1,
    DuplicateIgnored = 2,
    RejectedInsufficientStock = 3,
    RejectedInvalidProduct = 4
}
