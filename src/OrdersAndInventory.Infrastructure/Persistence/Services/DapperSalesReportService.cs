using Dapper;
using Microsoft.EntityFrameworkCore;
using OrdersAndInventory.Application.DTOs;
using OrdersAndInventory.Application.Services;

namespace OrdersAndInventory.Infrastructure.Persistence.Services;

public class DapperSalesReportService : ISalesReportService
{
    private readonly ApplicationDbContext _context;

    public DapperSalesReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DailySalesSummaryResponse> GetDailySalesSummaryAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var startUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtcExclusive = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }
        var isSqlServer = _context.Database.IsSqlServer();

        // Optimized aggregation query targeting IX_Orders_PlacedAtUtc and IX_OrderItems_OrderId_ProductSku
        var sql = isSqlServer
            ? @"SELECT 
                    CONVERT(VARCHAR(10), o.PlacedAtUtc, 23) AS [SalesDate],
                    oi.ProductSku AS [Sku],
                    SUM(oi.Quantity) AS [QuantitySold],
                    SUM(oi.TotalPrice) AS [GrossSales]
                FROM Orders o WITH (NOLOCK)
                INNER JOIN OrderItems oi WITH (NOLOCK) ON o.Id = oi.OrderId
                WHERE o.PlacedAtUtc >= @StartUtc AND o.PlacedAtUtc < @EndUtcExclusive
                GROUP BY CONVERT(VARCHAR(10), o.PlacedAtUtc, 23), oi.ProductSku
                ORDER BY CONVERT(VARCHAR(10), o.PlacedAtUtc, 23) ASC, oi.ProductSku ASC;"
            : @"SELECT 
                    strftime('%Y-%m-%d', o.PlacedAtUtc) AS [SalesDate],
                    oi.ProductSku AS [Sku],
                    SUM(oi.Quantity) AS [QuantitySold],
                    SUM(oi.TotalPrice) AS [GrossSales]
                FROM Orders o
                INNER JOIN OrderItems oi ON o.Id = oi.OrderId
                WHERE o.PlacedAtUtc >= @StartUtc AND o.PlacedAtUtc < @EndUtcExclusive
                GROUP BY strftime('%Y-%m-%d', o.PlacedAtUtc), oi.ProductSku
                ORDER BY strftime('%Y-%m-%d', o.PlacedAtUtc) ASC, oi.ProductSku ASC;";

        var command = new CommandDefinition(
            sql,
            new { StartUtc = startUtc, EndUtcExclusive = endUtcExclusive },
            cancellationToken: cancellationToken);

        var rawRows = (await connection.QueryAsync<SalesAggregateRow>(command)).ToList();

        var dailySummaries = rawRows
            .GroupBy(r => r.SalesDate)
            .Select(g => new DailySummaryDto(
                Date: g.Key,
                TotalQtySold: g.Sum(x => x.QuantitySold),
                TotalGrossSales: decimal.Round(g.Sum(x => x.GrossSales), 2, MidpointRounding.AwayFromZero),
                Products: g.Select(p => new DailyProductSalesDto(
                    p.Sku,
                    p.QuantitySold,
                    decimal.Round(p.GrossSales, 2, MidpointRounding.AwayFromZero)
                )).ToList()
            ))
            .ToList();

        return new DailySalesSummaryResponse(
            StartDate: startDate.ToString("yyyy-MM-dd"),
            EndDate: endDate.ToString("yyyy-MM-dd"),
            DailySummaries: dailySummaries);
    }

    private class SalesAggregateRow
    {
        public string SalesDate { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal GrossSales { get; set; }
    }
}
