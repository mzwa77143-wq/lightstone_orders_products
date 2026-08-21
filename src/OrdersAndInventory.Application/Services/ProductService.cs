using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAndInventory.Application.Common.Interfaces;
using OrdersAndInventory.Application.DTOs;
using OrdersAndInventory.Domain.Entities;
using OrdersAndInventory.Domain.Exceptions;

namespace OrdersAndInventory.Application.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ILogger<ProductService> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSku = request.Sku.Trim().ToUpperInvariant();

        var existingProduct = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == normalizedSku, cancellationToken);

        if (existingProduct != null)
        {
            throw new DomainValidationException($"Product with SKU '{normalizedSku}' already exists.");
        }

        var product = Product.Create(
            normalizedSku,
            request.Name,
            request.Price,
            request.Stock,
            _dateTimeProvider.UtcNow);

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new product {Sku} with initial stock {Stock}", product.Sku, product.Stock);

        return MapToResponse(product);
    }

    public async Task<ProductResponse> AdjustStockAsync(string sku, AdjustStockRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Sku == normalizedSku, cancellationToken);

        if (product == null)
        {
            throw new ProductNotFoundException(normalizedSku);
        }

        product.AdjustStock(request.Delta, _dateTimeProvider.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Adjusted stock for product {Sku} by {Delta}. New stock: {Stock}", product.Sku, request.Delta, product.Stock);

        return MapToResponse(product);
    }

    public async Task<ProductResponse?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == normalizedSku, cancellationToken);

        return product != null ? MapToResponse(product) : null;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Sku)
            .ToListAsync(cancellationToken);

        return products.Select(MapToResponse).ToList();
    }

    private static ProductResponse MapToResponse(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Sku,
            product.Name,
            product.Price,
            product.Stock,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
    }
}
