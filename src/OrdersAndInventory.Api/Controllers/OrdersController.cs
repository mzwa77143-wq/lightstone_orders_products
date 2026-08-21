using Microsoft.AspNetCore.Mvc;
using OrdersAndInventory.Application.DTOs;
using OrdersAndInventory.Application.Services;
using OrdersAndInventory.Domain.Enums;

namespace OrdersAndInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderProcessingService _orderProcessingService;

    public OrdersController(IOrderProcessingService orderProcessingService)
    {
        _orderProcessingService = orderProcessingService;
    }

    /// <summary>
    /// Submits a new order with atomic inventory deduction and idempotency verification.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubmitOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SubmitOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SubmitOrder(
        [FromBody] SubmitOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orderProcessingService.ProcessOrderAsync(request, cancellationToken);

        return result.Outcome switch
        {
            OrderProcessingOutcome.Accepted => StatusCode(StatusCodes.Status201Created, result.Order),
            OrderProcessingOutcome.DuplicateIgnored => Ok(result.Order),
            OrderProcessingOutcome.RejectedInsufficientStock => StatusCode(StatusCodes.Status422UnprocessableEntity, new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Insufficient Stock",
                Detail = result.ErrorMessage,
                Extensions =
                {
                    ["sku"] = result.FailedSku,
                    ["requestedQuantity"] = result.RequestedQuantity,
                    ["availableStock"] = result.AvailableStock
                }
            }),
            OrderProcessingOutcome.RejectedInvalidProduct => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Invalid Product SKU",
                Detail = result.ErrorMessage,
                Extensions =
                {
                    ["sku"] = result.FailedSku
                }
            }),
            _ => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Order Processing Error",
                Detail = result.ErrorMessage ?? "Unable to process order."
            })
        };
    }
}
