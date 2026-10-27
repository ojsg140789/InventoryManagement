using Inventory.Application.Exceptions;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Inventory.Infrastructure.Tests;

// Opt-in: requires a real SQL Server identity allowed to create/drop a disposable test database.
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INVENTORY_TEST_SQL_CONNECTION")))
            Skip = "Set INVENTORY_TEST_SQL_CONNECTION to run the real SQL Server integration test.";
    }
}

public sealed class ConcurrentOutputsTests
{
    [SqlServerFact]
    public async Task ConcurrentOutputs_ShouldCommitOnlyOneMovement_WhenLastUnitIsRequestedTwice()
    {
        var databaseName = "InventoryIntegration_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("INVENTORY_TEST_SQL_CONNECTION"))
        {
            InitialCatalog = databaseName
        };
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(connection.ConnectionString).Options;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var cancellationToken = timeout.Token;
        await using var setup = new InventoryDbContext(options);
        try
        {
            await setup.Database.MigrateAsync(cancellationToken);
            var product = new Product("RACE-001", "Concurrency test");
            var warehouse = new Warehouse("RACE-WH", "Concurrency warehouse");
            var stock = new Stock(product.Id, warehouse.Id);
            stock.Receive(1);
            setup.AddRange(product, warehouse, stock,
                new StockMovement(product.Id, warehouse.Id, StockMovementType.Entry, 1));
            await setup.SaveChangesAsync(cancellationToken);

            // Only the two command contexts use the barrier. Both finish reading and
            // applying Withdraw before either is allowed to send its writes.
            var barrier = new TwoWritersBarrier();
            var writerOptions = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlServer(connection.ConnectionString).AddInterceptors(barrier).Options;
            await using var first = new InventoryDbContext(writerOptions);
            await using var second = new InventoryDbContext(writerOptions);

            async Task<InventoryConflictException?> WithdrawAsync(InventoryDbContext context)
            {
                try
                {
                    await new StockMovementRepository(context).RegisterAsync(
                        new StockMovement(product.Id, warehouse.Id, StockMovementType.Exit, 1), cancellationToken);
                    return null;
                }
                catch (InventoryConflictException exception)
                {
                    return exception;
                }
            }

            var outcomes = await Task.WhenAll(WithdrawAsync(first), WithdrawAsync(second));
            Assert.Single(outcomes, outcome => outcome is null);
            Assert.Single(outcomes, outcome => outcome is not null);

            await using var verification = new InventoryDbContext(options);
            var savedStock = await verification.Stocks.AsNoTracking().SingleAsync(cancellationToken);
            var history = await verification.StockMovements.AsNoTracking().ToListAsync(cancellationToken);
            Assert.Equal(0, savedStock.Quantity);
            Assert.Equal(2, history.Count);
            Assert.Single(history, movement => movement.Type == StockMovementType.Exit);
            Assert.Equal(0, history.Sum(movement => movement.Type == StockMovementType.Entry
                ? movement.Quantity : -movement.Quantity));
        }
        finally
        {
            // Never use the supplied database name for cleanup. Only this generated,
            // isolated test database may be dropped, even if the test failed.
            if (setup.Database.GetDbConnection().Database != databaseName
                || !databaseName.StartsWith("InventoryIntegration_", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to clean up a non-test database.");
            await setup.Database.EnsureDeletedAsync(CancellationToken.None);
        }
    }

    private sealed class TwoWritersBarrier : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int writers;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref writers) == 2) ready.TrySetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            return result;
        }
    }
}
