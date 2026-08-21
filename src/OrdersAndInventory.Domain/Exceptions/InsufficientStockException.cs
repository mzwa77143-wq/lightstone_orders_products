namespace OrdersAndInventory.Domain.Exceptions;

public class InsufficientStockException : DomainException
{
    public string Sku { get; }
    public int RequestedQuantity { get; }
    public int AvailableStock { get; }

    public InsufficientStockException(string sku, int requestedQuantity, int availableStock)
        : base($"Insufficient stock for product '{sku}'. Requested: {requestedQuantity}, Available: {availableStock}.")
    {
        Sku = sku;
        RequestedQuantity = requestedQuantity;
        AvailableStock = availableStock;
    }

    public InsufficientStockException(string sku, int requestedQuantity)
        : base($"Insufficient stock for product '{sku}'. Requested quantity: {requestedQuantity}.")
    {
        Sku = sku;
        RequestedQuantity = requestedQuantity;
        AvailableStock = 0;
    }
}
