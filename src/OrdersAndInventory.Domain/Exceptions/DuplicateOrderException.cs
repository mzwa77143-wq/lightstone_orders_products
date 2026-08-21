namespace OrdersAndInventory.Domain.Exceptions;

public class DuplicateOrderException : DomainException
{
    public string ExternalOrderId { get; }

    public DuplicateOrderException(string externalOrderId)
        : base($"Order with ExternalOrderId '{externalOrderId}' already exists.")
    {
        ExternalOrderId = externalOrderId;
    }
}
