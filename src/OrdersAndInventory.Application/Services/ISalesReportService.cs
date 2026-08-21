using OrdersAndInventory.Application.DTOs;

namespace OrdersAndInventory.Application.Services;

public interface ISalesReportService
{
    Task<DailySalesSummaryResponse> GetDailySalesSummaryAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
