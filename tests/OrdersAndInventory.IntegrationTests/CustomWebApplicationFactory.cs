using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrdersAndInventory.Domain.Entities;
using OrdersAndInventory.Infrastructure.Persistence;

namespace OrdersAndInventory.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbFileName = $"test_orders_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection));

            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            var connectionString = $"Data Source={_dbFileName};Foreign Keys=True;";

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlite(connectionString);
            });

            // Build service provider and seed testing data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Seed initial products for integration tests
            if (!db.Products.Any())
            {
                var now = DateTime.UtcNow;
                db.Products.AddRange(
                    Product.Create("TEST-LAPTOP", "Test Gaming Laptop", 999.99m, 10, now),
                    Product.Create("TEST-MOUSE", "Test Wireless Mouse", 25.00m, 50, now),
                    Product.Create("TEST-SCARCE", "Scarce Limited Item", 100.00m, 3, now),
                    Product.Create("TEST-OUTOFSTOCK", "Out of stock Item", 15.00m, 0, now)
                );
                db.SaveChanges();
            }
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(_dbFileName))
            {
                SqliteConnection.ClearAllPools();
                File.Delete(_dbFileName);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
