using OrdersAndInventory.Application.DTOs;

namespace OrdersAndInventory.Application.Services;

public interface IOrderProcessingService
{
    Task<ProcessOrderResult> ProcessOrderAsync(SubmitOrderRequest request, CancellationToken cancellationToken = default);
}
