using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrdersAndInventory.Application.DTOs;

namespace OrdersAndInventory.IntegrationTests.Controllers;

public class OrdersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SubmitOrder_ValidOrder_ShouldReturn201AndDeductStock()
    {
        // 1. Check initial stock of product
        var productBefore = await _client.GetFromJsonAsync<ProductResponse>("/api/products/TEST-LAPTOP");
        var initialStock = productBefore!.Stock;

        // 2. Submit order for 2 laptops
        var externalOrderId = $"EXT-ORDER-{Guid.NewGuid():N}";
        var request = new SubmitOrderRequest(
            ExternalOrderId: externalOrderId,
            PlacedAtUtc: DateTime.UtcNow,
            Items: new List<SubmitOrderItemRequest>
            {
                new("TEST-LAPTOP", 2, 999.99m)
            });

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderResponse = await response.Content.ReadFromJsonAsync<SubmitOrderResponse>();
        orderResponse.Should().NotBeNull();
        orderResponse!.ExternalOrderId.Should().Be(externalOrderId);
        orderResponse.IsDuplicate.Should().BeFalse();
        orderResponse.TotalAmount.Should().Be(1999.98m);

        // 3. Verify stock was decremented
        var productAfter = await _client.GetFromJsonAsync<ProductResponse>("/api/products/TEST-LAPTOP");
        productAfter!.Stock.Should().Be(initialStock - 2);
    }

    [Fact]
    public async Task SubmitOrder_DuplicateExternalOrderId_ShouldReturn200WithIsDuplicateTrueAndNotDeductStockAgain()
    {
        // 1. Submit original order
        var externalOrderId = $"EXT-DUP-TEST-{Guid.NewGuid():N}";
        var request = new SubmitOrderRequest(
            ExternalOrderId: externalOrderId,
            PlacedAtUtc: DateTime.UtcNow,
            Items: new List<SubmitOrderItemRequest>
            {
                new("TEST-MOUSE", 1, 25.00m)
            });

        var firstResponse = await _client.PostAsJsonAsync("/api/orders", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var productAfterFirst = await _client.GetFromJsonAsync<ProductResponse>("/api/products/TEST-MOUSE");
        var stockAfterFirst = productAfterFirst!.Stock;

        // 2. Re-submit the exact same order
        var duplicateResponse = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var duplicateBody = await duplicateResponse.Content.ReadFromJsonAsync<SubmitOrderResponse>();
        duplicateBody.Should().NotBeNull();
        duplicateBody!.ExternalOrderId.Should().Be(externalOrderId);
        duplicateBody.IsDuplicate.Should().BeTrue();

        // 3. Verify stock was NOT deducted again
        var productAfterDuplicate = await _client.GetFromJsonAsync<ProductResponse>("/api/products/TEST-MOUSE");
        productAfterDuplicate!.Stock.Should().Be(stockAfterFirst);
    }

    [Fact]
    public async Task SubmitOrder_InsufficientStock_ShouldReturn422AndNotDeductStock()
    {
        // Arrange
        var externalOrderId = $"EXT-INSUFF-{Guid.NewGuid():N}";
        var request = new SubmitOrderRequest(
            ExternalOrderId: externalOrderId,
            PlacedAtUtc: DateTime.UtcNow,
            Items: new List<SubmitOrderItemRequest>
            {
                new("TEST-OUTOFSTOCK", 5, 15.00m)
            });

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // Verify product stock is still 0
        var product = await _client.GetFromJsonAsync<ProductResponse>("/api/products/TEST-OUTOFSTOCK");
        product!.Stock.Should().Be(0);
    }

    [Fact]
    public async Task SubmitOrder_ConcurrentRequestsForScarceStock_ShouldPreventOverselling()
    {
        // 1. Create a product with exactly 3 in stock
        var scarceSku = $"CONCURRENT-SKU-{Guid.NewGuid():N}";
        var createResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest(scarceSku, "Limited Edition", 50m, 3));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Launch 10 concurrent order requests, each requesting 1 item
        var concurrentTasks = Enumerable.Range(1, 10).Select(async i =>
        {
            var req = new SubmitOrderRequest(
                ExternalOrderId: $"CONC-ORD-{scarceSku}-{i}",
                PlacedAtUtc: DateTime.UtcNow,
                Items: new List<SubmitOrderItemRequest>
                {
                    new(scarceSku, 1, 50m)
                });

            return await _client.PostAsJsonAsync("/api/orders", req);
        }).ToList();

        var results = await Task.WhenAll(concurrentTasks);

        // 3. Verify exactly 3 succeeded (201 Created) and 7 were rejected (422 Unprocessable Entity)
        var successfulOrders = results.Count(r => r.StatusCode == HttpStatusCode.Created);
        var rejectedOrders = results.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity);

        successfulOrders.Should().Be(3);
        rejectedOrders.Should().Be(7);

        // 4. Verify final stock is exactly 0 and never negative
        var finalProduct = await _client.GetFromJsonAsync<ProductResponse>($"/api/products/{scarceSku}");
        finalProduct!.Stock.Should().Be(0);
    }
}
