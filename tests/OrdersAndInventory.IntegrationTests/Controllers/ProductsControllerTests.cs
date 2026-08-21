using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrdersAndInventory.Application.DTOs;

namespace OrdersAndInventory.IntegrationTests.Controllers;

public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_WithValidPayload_ShouldReturn201Created()
    {
        // Arrange
        var sku = $"NEW-PROD-{Guid.NewGuid():N}";
        var request = new CreateProductRequest(sku, "Brand New Gadget", 79.99m, 30);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ProductResponse>();
        created.Should().NotBeNull();
        created!.Sku.Should().Be(sku.ToUpperInvariant());
        created.Stock.Should().Be(30);
    }

    [Fact]
    public async Task GetProductBySku_ExistingSku_ShouldReturn200Ok()
    {
        // Act
        var response = await _client.GetAsync("/api/products/TEST-LAPTOP");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        product.Should().NotBeNull();
        product!.Sku.Should().Be("TEST-LAPTOP");
    }

    [Fact]
    public async Task GetProductBySku_NonExistentSku_ShouldReturn404NotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/products/NON-EXISTENT-SKU");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdjustStock_ValidDelta_ShouldReturnUpdatedStock()
    {
        // Arrange
        var request = new AdjustStockRequest(15);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products/TEST-MOUSE/adjust-stock", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();
        updated.Should().NotBeNull();
        updated!.Stock.Should().BeGreaterThan(50);
    }
}
