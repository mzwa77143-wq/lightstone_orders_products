namespace OrdersAndInventory.Application.Common.Interfaces;

public interface IInventoryRepository
{
    /// <summary>
    /// Atomically deducts inventory for a product SKU using DB row-level locking within the current transaction.
    /// Returns true if deduction succeeded, false if insufficient stock or product not found.
    /// </summary>
    Task<bool> TryDeductStockAtomicAsync(
        string sku,
        int quantity,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks available stock for a product SKU.
    /// </summary>
    Task<int?> GetStockAsync(
        string sku,
        CancellationToken cancellationToken = default);
}
