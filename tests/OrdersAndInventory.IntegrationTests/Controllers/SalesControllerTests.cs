using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrdersAndInventory.Application.DTOs;

namespace OrdersAndInventory.IntegrationTests.Controllers;

public class SalesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SalesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDailySummary_WithValidDateRange_ShouldReturnAggregatedSales()
    {
        // 1. Submit orders for a specific date
        var targetDate = new DateTime(2026, 8, 15, 14, 30, 0, DateTimeKind.Utc);
        var order1 = new SubmitOrderRequest(
            ExternalOrderId: $"SALES-ORD-1-{Guid.NewGuid():N}",
            PlacedAtUtc: targetDate,
            Items: new List<SubmitOrderItemRequest>
            {
                new("TEST-LAPTOP", 1, 1000m)
            });

        var order2 = new SubmitOrderRequest(
            ExternalOrderId: $"SALES-ORD-2-{Guid.NewGuid():N}",
            PlacedAtUtc: targetDate.AddHours(2),
            Items: new List<SubmitOrderItemRequest>
            {
                new("TEST-MOUSE", 2, 25m)
            });

        await _client.PostAsJsonAsync("/api/orders", order1);
        await _client.PostAsJsonAsync("/api/orders", order2);

        // 2. Query summary for that date
        var response = await _client.GetAsync("/api/sales/daily-summary?startDate=2026-08-15&endDate=2026-08-15");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<DailySalesSummaryResponse>();
        summary.Should().NotBeNull();
        summary!.StartDate.Should().Be("2026-08-15");
        summary.EndDate.Should().Be("2026-08-15");
        summary.DailySummaries.Should().NotBeEmpty();

        var daySummary = summary.DailySummaries.FirstOrDefault(s => s.Date == "2026-08-15");
        daySummary.Should().NotBeNull();
        daySummary!.TotalQtySold.Should().BeGreaterThanOrEqualTo(3);
        daySummary.TotalGrossSales.Should().BeGreaterThanOrEqualTo(1050m);
    }

    [Fact]
    public async Task GetDailySummary_WithInvalidDateFormat_ShouldReturn400BadRequest()
    {
        var response = await _client.GetAsync("/api/sales/daily-summary?startDate=invalid-date&endDate=2026-08-15");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
