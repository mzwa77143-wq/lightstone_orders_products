using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAndInventory.Application.Common.Interfaces;
using OrdersAndInventory.Application.Services;
using OrdersAndInventory.Domain.Entities;

namespace OrdersAndInventory.Infrastructure.Persistence;

public class DataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        ApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;

        if (!await _context.Products.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database is empty. Seeding initial products...");

            var initialProducts = new List<Product>
            {
                Product.Create("LAPTOP-001", "UltraBook Pro 15-inch M3", 1499.99m, 50, now),
                Product.Create("MOUSE-002", "Wireless Ergonomic Precision Mouse", 49.99m, 200, now),
                Product.Create("KEYBOARD-003", "Mechanical RGB Gaming Keyboard", 119.50m, 100, now),
                Product.Create("MONITOR-004", "4K UHD 27-inch IPS Monitor", 399.00m, 75, now),
                Product.Create("HEADSET-005", "Active Noise Cancelling Wireless Headset", 89.99m, 150, now),
                Product.Create("WEBCAM-006", "1080p Full HD Pro Streaming Webcam", 59.99m, 80, now),
                Product.Create("DOCK-007", "Thunderbolt 4 Multi-Port Docking Station", 179.00m, 40, now),
                Product.Create("CABLE-008", "Braided USB-C to USB-C 100W Cable 2m", 14.99m, 500, now)
            };

            _context.Products.AddRange(initialProducts);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully seeded {Count} products into the database.", initialProducts.Count);
        }

        if (!await _context.Orders.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("No orders found. Seeding initial sample orders...");

            var sampleOrders = new List<Order>
            {
                Order.Create("ORD-20260820-001", new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc), new List<(string, int, decimal)>
                {
                    ("LAPTOP-001", 1, 1499.99m),
                    ("MOUSE-002", 1, 49.99m)
                }, now),
                Order.Create("ORD-20260820-002", new DateTime(2026, 8, 20, 14, 30, 0, DateTimeKind.Utc), new List<(string, int, decimal)>
                {
                    ("KEYBOARD-003", 2, 119.50m),
                    ("HEADSET-005", 1, 89.99m)
                }, now),
                Order.Create("ORD-20260821-001", new DateTime(2026, 8, 21, 9, 15, 0, DateTimeKind.Utc), new List<(string, int, decimal)>
                {
                    ("MONITOR-004", 2, 399.00m),
                    ("DOCK-007", 1, 179.00m),
                    ("CABLE-008", 3, 14.99m)
                }, now),
                Order.Create("ORD-20260821-002", new DateTime(2026, 8, 21, 16, 45, 0, DateTimeKind.Utc), new List<(string, int, decimal)>
                {
                    ("WEBCAM-006", 1, 59.99m),
                    ("HEADSET-005", 1, 89.99m),
                    ("MOUSE-002", 2, 49.99m)
                }, now),
                Order.Create("ORD-20260822-001", new DateTime(2026, 8, 22, 8, 0, 0, DateTimeKind.Utc), new List<(string, int, decimal)>
                {
                    ("LAPTOP-001", 1, 1499.99m),
                    ("DOCK-007", 1, 179.00m),
                    ("KEYBOARD-003", 1, 119.50m),
                    ("MOUSE-002", 1, 49.99m)
                }, now)
            };

            _context.Orders.AddRange(sampleOrders);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully seeded {Count} sample orders into the database.", sampleOrders.Count);
        }
    }
}
