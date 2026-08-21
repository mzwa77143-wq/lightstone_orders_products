using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrdersAndInventory.Api.Middlewares;
using OrdersAndInventory.Application;
using OrdersAndInventory.Application.Services;
using OrdersAndInventory.Infrastructure;
using OrdersAndInventory.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with structured JSON logging
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter());
});

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Orders and Inventory Service API",
        Version = "v1",
        Description = "Production-ready clean architecture API for high-concurrency order processing and inventory management."
    });
});

// Add Clean Architecture layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Orders and Inventory API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map Health Checks
// Liveness Check: returns 200 Healthy if API is responding
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

// Readiness Check: runs checks tagged with "ready" (e.g. SQL Server connection)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapControllers();

// Execute Database Migration / Seeder on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        
        // When running in development, ensure database is migrated or created
        if (dbContext.Database.IsSqlServer())
        {
            logger.LogInformation("Applying SQL Server database migrations if pending...");
            try
            {
                await dbContext.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Database migration failed on startup. Ensuring created as fallback...");
                await dbContext.Database.EnsureCreatedAsync();
            }
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            var seeder = services.GetRequiredService<IDataSeeder>();
            await seeder.SeedAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "An error occurred during database initialization / seeding.");
    }
}

app.Run();

// Partial Program class for WebApplicationFactory in Integration Tests
public partial class Program { }
