namespace OrdersAndInventory.Domain.Exceptions;

public class ProductNotFoundException : DomainException
{
    public string Sku { get; }

    public ProductNotFoundException(string sku)
        : base($"Product with SKU '{sku}' was not found.")
    {
        Sku = sku;
    }
}
