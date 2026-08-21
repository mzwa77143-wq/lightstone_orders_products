using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrdersAndInventory.Application.Common.Interfaces;
using OrdersAndInventory.Application.Services;
using OrdersAndInventory.Application.Validators;

namespace OrdersAndInventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddValidatorsFromAssemblyContaining<SubmitOrderRequestValidator>();

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderProcessingService, OrderProcessingService>();

        return services;
    }
}
