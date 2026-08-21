namespace OrdersAndInventory.Application.Services;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
