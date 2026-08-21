using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrdersAndInventory.Application.Common.Interfaces;
using OrdersAndInventory.Application.Services;
using OrdersAndInventory.Infrastructure.Persistence;
using OrdersAndInventory.Infrastructure.Persistence.Repositories;
using OrdersAndInventory.Infrastructure.Persistence.Services;

namespace OrdersAndInventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=OrdersAndInventoryDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ISalesReportService, DapperSalesReportService>();
        services.AddScoped<IDataSeeder, DataSeeder>();

        // Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(
                name: "sqlserver",
                tags: new[] { "ready" });

        return services;
    }
}
