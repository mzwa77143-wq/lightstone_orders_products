using OrdersAndInventory.Domain.Enums;
using OrdersAndInventory.Domain.Exceptions;

namespace OrdersAndInventory.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _items = new();

    public Guid Id { get; private set; }
    public string ExternalOrderId { get; private set; } = string.Empty;
    public DateTime PlacedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // EF Core constructor
    private Order() { }

    public static Order Create(
        string externalOrderId,
        DateTime placedAtUtc,
        IEnumerable<(string Sku, int Quantity, decimal UnitPrice)> items,
        DateTime? createdAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(externalOrderId))
            throw new DomainValidationException("ExternalOrderId cannot be null or empty.");

        var itemList = items?.ToList() ?? new List<(string, int, decimal)>();
        if (!itemList.Any())
            throw new DomainValidationException("An order must contain at least one item.");

        var now = createdAtUtc ?? DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ExternalOrderId = externalOrderId.Trim(),
            PlacedAtUtc = DateTime.SpecifyKind(placedAtUtc, DateTimeKind.Utc),
            CreatedAtUtc = now,
            Status = OrderStatus.Accepted
        };

        foreach (var item in itemList)
        {
            var orderItem = OrderItem.Create(order.Id, item.Sku, item.Quantity, item.UnitPrice);
            order._items.Add(orderItem);
        }

        order.TotalAmount = decimal.Round(order._items.Sum(i => i.TotalPrice), 2, MidpointRounding.AwayFromZero);

        return order;
    }

    public void MarkCompleted()
    {
        Status = OrderStatus.Completed;
    }

    public void MarkCancelled()
    {
        Status = OrderStatus.Cancelled;
    }
}
