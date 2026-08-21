using OrdersAndInventory.Application.DTOs;

namespace OrdersAndInventory.Application.Services;

public interface IProductService
{
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> AdjustStockAsync(string sku, AdjustStockRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductResponse>> GetAllProductsAsync(CancellationToken cancellationToken = default);
}
