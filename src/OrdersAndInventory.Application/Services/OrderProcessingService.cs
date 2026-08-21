using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAndInventory.Application.Common.Interfaces;
using OrdersAndInventory.Application.DTOs;
using OrdersAndInventory.Domain.Entities;
using OrdersAndInventory.Domain.Enums;
using OrdersAndInventory.Domain.Exceptions;

namespace OrdersAndInventory.Application.Services;

public class OrderProcessingService : IOrderProcessingService
{
    private readonly IApplicationDbContext _context;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<SubmitOrderRequest> _validator;
    private readonly ILogger<OrderProcessingService> _logger;

    public OrderProcessingService(
        IApplicationDbContext context,
        IInventoryRepository inventoryRepository,
        IDateTimeProvider dateTimeProvider,
        IValidator<SubmitOrderRequest> validator,
        ILogger<OrderProcessingService> logger)
    {
        _context = context;
        _inventoryRepository = inventoryRepository;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ProcessOrderResult> ProcessOrderAsync(SubmitOrderRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Order validation failed for ExternalOrderId {ExternalOrderId}: {Errors}",
                request.ExternalOrderId, errors);
            throw new DomainValidationException(errors);
        }

        var externalOrderId = request.ExternalOrderId.Trim();

        var strategy = _context.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // 2. Begin explicit database transaction for concurrency & idempotency safety
            await using var transaction = await _context.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            try
            {
                // 3. Idempotency Check: check if order with same ExternalOrderId already exists
                var existingOrder = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.ExternalOrderId == externalOrderId, cancellationToken);

                if (existingOrder != null)
                {
                    _logger.LogInformation(
                        "Order processing outcome: {Outcome}. ExternalOrderId: {ExternalOrderId}, ExistingOrderId: {OrderId}, TotalAmount: {TotalAmount}",
                        OrderProcessingOutcome.DuplicateIgnored.ToString(),
                        existingOrder.ExternalOrderId,
                        existingOrder.Id,
                        existingOrder.TotalAmount);

                    await transaction.RollbackAsync(cancellationToken);

                    return new ProcessOrderResult(
                        OrderProcessingOutcome.DuplicateIgnored,
                        MapToResponse(existingOrder, isDuplicate: true),
                        null);
                }

                // 4. Consolidate requested quantities by SKU and sort alphabetically to prevent deadlock
                var consolidatedItems = request.Items
                    .GroupBy(i => i.Sku.Trim().ToUpperInvariant())
                    .Select(g => new
                    {
                        Sku = g.Key,
                        TotalQuantity = g.Sum(x => x.Quantity),
                        UnitPrice = g.First().UnitPrice
                    })
                    .OrderBy(x => x.Sku)
                    .ToList();

                var now = _dateTimeProvider.UtcNow;

                // 5. Attempt atomic inventory deductions for each SKU
                foreach (var item in consolidatedItems)
                {
                    var deducted = await _inventoryRepository.TryDeductStockAtomicAsync(
                        item.Sku,
                        item.TotalQuantity,
                        now,
                        cancellationToken);

                    if (!deducted)
                    {
                        // Check whether SKU is missing or simply has insufficient stock
                        var availableStock = await _inventoryRepository.GetStockAsync(item.Sku, cancellationToken);

                        await transaction.RollbackAsync(cancellationToken);

                        if (!availableStock.HasValue)
                        {
                            _logger.LogWarning(
                                "Order processing outcome: {Outcome}. ExternalOrderId: {ExternalOrderId}, Sku: {Sku}",
                                OrderProcessingOutcome.RejectedInvalidProduct.ToString(),
                                externalOrderId,
                                item.Sku);

                            return new ProcessOrderResult(
                                OrderProcessingOutcome.RejectedInvalidProduct,
                                null,
                                $"Product SKU '{item.Sku}' does not exist.",
                                FailedSku: item.Sku,
                                RequestedQuantity: item.TotalQuantity,
                                AvailableStock: null);
                        }

                        _logger.LogWarning(
                            "Order processing outcome: {Outcome}. ExternalOrderId: {ExternalOrderId}, Sku: {Sku}, RequestedQty: {RequestedQuantity}, AvailableStock: {AvailableStock}",
                            OrderProcessingOutcome.RejectedInsufficientStock.ToString(),
                            externalOrderId,
                            item.Sku,
                            item.TotalQuantity,
                            availableStock.Value);

                        return new ProcessOrderResult(
                            OrderProcessingOutcome.RejectedInsufficientStock,
                            null,
                            $"Insufficient stock for product '{item.Sku}'. Requested: {item.TotalQuantity}, Available: {availableStock.Value}.",
                            FailedSku: item.Sku,
                            RequestedQuantity: item.TotalQuantity,
                            AvailableStock: availableStock.Value);
                    }
                }

                // 6. Create Order and OrderItems entities
                var orderItems = request.Items.Select(i => (
                    Sku: i.Sku.Trim().ToUpperInvariant(),
                    Quantity: i.Quantity,
                    UnitPrice: i.UnitPrice
                )).ToList();

                var order = Order.Create(
                    externalOrderId,
                    request.PlacedAtUtc,
                    orderItems,
                    now);

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(cancellationToken);

                // 7. Commit Transaction
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Order processing outcome: {Outcome}. ExternalOrderId: {ExternalOrderId}, OrderId: {OrderId}, TotalAmount: {TotalAmount}, ItemCount: {ItemCount}",
                    OrderProcessingOutcome.Accepted.ToString(),
                    order.ExternalOrderId,
                    order.Id,
                    order.TotalAmount,
                    order.Items.Count);

                return new ProcessOrderResult(
                    OrderProcessingOutcome.Accepted,
                    MapToResponse(order, isDuplicate: false),
                    null);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                await transaction.RollbackAsync(cancellationToken);

                // Fetch the existing order created concurrently
                var existingOrder = await _context.Orders
                    .Include(o => o.Items)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.ExternalOrderId == externalOrderId, cancellationToken);

                if (existingOrder != null)
                {
                    _logger.LogInformation(
                        "Order processing outcome: {Outcome} (concurrency resolution). ExternalOrderId: {ExternalOrderId}, ExistingOrderId: {OrderId}",
                        OrderProcessingOutcome.DuplicateIgnored.ToString(),
                        existingOrder.ExternalOrderId,
                        existingOrder.Id);

                    return new ProcessOrderResult(
                        OrderProcessingOutcome.DuplicateIgnored,
                        MapToResponse(existingOrder, isDuplicate: true),
                        null);
                }

                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Unexpected error processing order for ExternalOrderId {ExternalOrderId}", externalOrderId);
                throw;
            }
        });
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
    }

    private static SubmitOrderResponse MapToResponse(Order order, bool isDuplicate)
    {
        var items = order.Items.Select(i => new OrderItemResponse(
            i.Id,
            i.ProductSku,
            i.Quantity,
            i.UnitPrice,
            i.TotalPrice
        )).ToList();

        return new SubmitOrderResponse(
            order.Id,
            order.ExternalOrderId,
            order.PlacedAtUtc,
            order.TotalAmount,
            order.Status.ToString(),
            items,
            isDuplicate);
    }
}
