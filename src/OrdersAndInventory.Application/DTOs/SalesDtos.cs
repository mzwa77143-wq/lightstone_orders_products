using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OrdersAndInventory.Application.DTOs;

public record DailySalesSummaryResponse(
    [property: JsonPropertyName("start_date"), JsonProperty("start_date")]
    string StartDate,

    [property: JsonPropertyName("end_date"), JsonProperty("end_date")]
    string EndDate,

    [property: JsonPropertyName("daily_summaries"), JsonProperty("daily_summaries")]
    List<DailySummaryDto> DailySummaries);

public record DailySummaryDto(
    [property: JsonPropertyName("date"), JsonProperty("date")]
    string Date,

    [property: JsonPropertyName("total_qty_sold"), JsonProperty("total_qty_sold")]
    int TotalQtySold,

    [property: JsonPropertyName("total_gross_sales"), JsonProperty("total_gross_sales")]
    decimal TotalGrossSales,

    [property: JsonPropertyName("products"), JsonProperty("products")]
    List<DailyProductSalesDto> Products);

public record DailyProductSalesDto(
    [property: JsonPropertyName("sku"), JsonProperty("sku")]
    string Sku,

    [property: JsonPropertyName("qty_sold"), JsonProperty("qty_sold")]
    int QtySold,

    [property: JsonPropertyName("gross_sales"), JsonProperty("gross_sales")]
    decimal GrossSales);
