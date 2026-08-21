using Microsoft.AspNetCore.Mvc;
using OrdersAndInventory.Application.DTOs;
using OrdersAndInventory.Application.Services;

namespace OrdersAndInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _productService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProductBySku), new { sku = product.Sku }, product);
    }

    /// <summary>
    /// Retrieves all products.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProducts(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllProductsAsync(cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Retrieves a single product by SKU.
    /// </summary>
    [HttpGet("{sku}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductBySku([FromRoute] string sku, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductBySkuAsync(sku, cancellationToken);
        if (product == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Product Not Found",
                Detail = $"Product with SKU '{sku}' was not found."
            });
        }

        return Ok(product);
    }

    /// <summary>
    /// Adjusts available stock for a product.
    /// </summary>
    [HttpPost("{sku}/adjust-stock")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AdjustStock(
        [FromRoute] string sku,
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.AdjustStockAsync(sku, request, cancellationToken);
        return Ok(product);
    }
}
