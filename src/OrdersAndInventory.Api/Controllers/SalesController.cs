using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using OrdersAndInventory.Application.DTOs;
using OrdersAndInventory.Application.Services;

namespace OrdersAndInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SalesController : ControllerBase
{
    private readonly ISalesReportService _salesReportService;

    public SalesController(ISalesReportService salesReportService)
    {
        _salesReportService = salesReportService;
    }

    /// <summary>
    /// Retrieves aggregated daily sales grouped by date and SKU.
    /// </summary>
    /// <param name="startDate">Start date in yyyy-MM-dd format</param>
    /// <param name="endDate">End date in yyyy-MM-dd format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("daily-summary")]
    [ProducesResponseType(typeof(DailySalesSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDailySummary(
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(startDate) || !DateOnly.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStartDate))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Date Parameter",
                Detail = "The 'startDate' query parameter is required and must be in 'yyyy-MM-dd' format."
            });
        }

        if (string.IsNullOrWhiteSpace(endDate) || !DateOnly.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEndDate))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Date Parameter",
                Detail = "The 'endDate' query parameter is required and must be in 'yyyy-MM-dd' format."
            });
        }

        if (parsedEndDate < parsedStartDate)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Date Range",
                Detail = "'endDate' cannot be earlier than 'startDate'."
            });
        }

        var result = await _salesReportService.GetDailySalesSummaryAsync(parsedStartDate, parsedEndDate, cancellationToken);
        return Ok(result);
    }
}
