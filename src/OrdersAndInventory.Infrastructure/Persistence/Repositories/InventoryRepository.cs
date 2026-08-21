using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrdersAndInventory.Application.Common.Interfaces;

namespace OrdersAndInventory.Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly ApplicationDbContext _context;

    public InventoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryDeductStockAtomicAsync(
        string sku,
        int quantity,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }

        var currentTransaction = _context.Database.CurrentTransaction?.GetDbTransaction();
        var isSqlServer = _context.Database.IsSqlServer();

        // Use SQL Server table hints for explicit row-level exclusive locking when on SQL Server
        var sql = isSqlServer
            ? @"UPDATE Products WITH (UPDLOCK, ROWLOCK)
                SET Stock = Stock - @Quantity, UpdatedAtUtc = @UpdatedAtUtc
                WHERE Sku = @Sku AND Stock >= @Quantity;"
            : @"UPDATE Products
                SET Stock = Stock - @Quantity, UpdatedAtUtc = @UpdatedAtUtc
                WHERE Sku = @Sku AND Stock >= @Quantity;";

        var command = new CommandDefinition(
            sql,
            new { Sku = sku, Quantity = quantity, UpdatedAtUtc = updatedAtUtc },
            transaction: currentTransaction,
            cancellationToken: cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(command);
        return rowsAffected > 0;
    }

    public async Task<int?> GetStockAsync(
        string sku,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }

        var currentTransaction = _context.Database.CurrentTransaction?.GetDbTransaction();
        var isSqlServer = _context.Database.IsSqlServer();

        var sql = isSqlServer
            ? "SELECT Stock FROM Products WITH (NOLOCK) WHERE Sku = @Sku;"
            : "SELECT Stock FROM Products WHERE Sku = @Sku;";

        var command = new CommandDefinition(
            sql,
            new { Sku = sku },
            transaction: currentTransaction,
            cancellationToken: cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<int?>(command);
    }
}
