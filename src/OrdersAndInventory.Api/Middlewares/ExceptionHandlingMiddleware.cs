using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OrdersAndInventory.Domain.Exceptions;

namespace OrdersAndInventory.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred while processing request: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path,
            Status = (int)statusCode,
            Title = "An error occurred while processing your request."
        };

        switch (exception)
        {
            case DomainValidationException validationEx:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Validation Error";
                problemDetails.Detail = validationEx.Message;
                break;

            case ProductNotFoundException notFoundEx:
                statusCode = HttpStatusCode.NotFound;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Resource Not Found";
                problemDetails.Detail = notFoundEx.Message;
                problemDetails.Extensions["sku"] = notFoundEx.Sku;
                break;

            case InsufficientStockException stockEx:
                statusCode = HttpStatusCode.UnprocessableEntity;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Insufficient Stock";
                problemDetails.Detail = stockEx.Message;
                problemDetails.Extensions["sku"] = stockEx.Sku;
                problemDetails.Extensions["requestedQuantity"] = stockEx.RequestedQuantity;
                problemDetails.Extensions["availableStock"] = stockEx.AvailableStock;
                break;

            case DuplicateOrderException dupEx:
                statusCode = HttpStatusCode.Conflict;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Duplicate Order";
                problemDetails.Detail = dupEx.Message;
                problemDetails.Extensions["externalOrderId"] = dupEx.ExternalOrderId;
                break;

            case FormatException formatEx:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Invalid Parameter Format";
                problemDetails.Detail = formatEx.Message;
                break;

            case ArgumentException argEx:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Status = (int)statusCode;
                problemDetails.Title = "Bad Request";
                problemDetails.Detail = argEx.Message;
                break;

            default:
                problemDetails.Detail = "An unexpected internal server error occurred.";
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(Newtonsoft.Json.JsonConvert.SerializeObject(problemDetails));
    }
}
