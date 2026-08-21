using System.Text.Json.Serialization;
using Newtonsoft.Json;
using OrdersAndInventory.Domain.Enums;

namespace OrdersAndInventory.Application.DTOs;

public record SubmitOrderRequest(
    [property: JsonPropertyName("external_order_id"), JsonProperty("external_order_id")]
    string ExternalOrderId,

    [property: JsonPropertyName("placed_at"), JsonProperty("placed_at")]
    DateTime PlacedAtUtc,

    [property: JsonPropertyName("items"), JsonProperty("items")]
    List<SubmitOrderItemRequest> Items);

public record SubmitOrderItemRequest(
    [property: JsonPropertyName("sku"), JsonProperty("sku")]
    string Sku,

    [property: JsonPropertyName("qty"), JsonProperty("qty")]
    int Quantity,

    [property: JsonPropertyName("unit_price"), JsonProperty("unit_price")]
    decimal UnitPrice);

public record SubmitOrderResponse(
    [property: JsonPropertyName("id"), JsonProperty("id")]
    Guid Id,

    [property: JsonPropertyName("external_order_id"), JsonProperty("external_order_id")]
    string ExternalOrderId,

    [property: JsonPropertyName("placed_at"), JsonProperty("placed_at")]
    DateTime PlacedAtUtc,

    [property: JsonPropertyName("total_amount"), JsonProperty("total_amount")]
    decimal TotalAmount,

    [property: JsonPropertyName("status"), JsonProperty("status")]
    string Status,

    [property: JsonPropertyName("items"), JsonProperty("items")]
    List<OrderItemResponse> Items,

    [property: JsonPropertyName("is_duplicate"), JsonProperty("is_duplicate")]
    bool IsDuplicate);

public record OrderItemResponse(
    [property: JsonPropertyName("id"), JsonProperty("id")]
    Guid Id,

    [property: JsonPropertyName("sku"), JsonProperty("sku")]
    string Sku,

    [property: JsonPropertyName("qty"), JsonProperty("qty")]
    int Quantity,

    [property: JsonPropertyName("unit_price"), JsonProperty("unit_price")]
    decimal UnitPrice,

    [property: JsonPropertyName("total_price"), JsonProperty("total_price")]
    decimal TotalPrice);

public record ProcessOrderResult(
    OrderProcessingOutcome Outcome,
    SubmitOrderResponse? Order,
    string? ErrorMessage,
    string? FailedSku = null,
    int? RequestedQuantity = null,
    int? AvailableStock = null);
