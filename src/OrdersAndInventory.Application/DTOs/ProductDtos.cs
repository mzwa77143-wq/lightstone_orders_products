namespace OrdersAndInventory.Application.DTOs;

public record CreateProductRequest(
    string Sku,
    string Name,
    decimal Price,
    int Stock);

public record AdjustStockRequest(
    int Delta);

public record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    decimal Price,
    int Stock,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
