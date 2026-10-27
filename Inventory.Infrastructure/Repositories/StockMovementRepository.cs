using System.Data;
using Inventory.Application.Abstractions;
using Inventory.Application.Exceptions;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public sealed class StockMovementRepository(InventoryDbContext context) : IStockMovementRepository
{
    public async Task RegisterAsync(StockMovement movement, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            // Tracked read retains the original rowversion for the UPDATE predicate.
            var stock = await context.Stocks.SingleOrDefaultAsync(
                x => x.ProductId == movement.ProductId && x.WarehouseId == movement.WarehouseId, cancellationToken);

            if (stock is null)
            {
                if (movement.Type == StockMovementType.Exit)
                    throw new InventoryConflictException("Not enough items.");
                stock = new Stock(movement.ProductId, movement.WarehouseId);
                context.Stocks.Add(stock);
            }

            if (movement.Type == StockMovementType.Entry)
            {
                stock.Receive(movement.Quantity);
            }
            else
            {
                if (stock.Quantity < movement.Quantity)
                    throw new InventoryConflictException("Not enough items.");
                stock.Withdraw(movement.Quantity);
            }

            context.StockMovements.Add(movement);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            // Disposing the transaction rolls back stock AND history before the HTTP response.
            throw PersistenceErrors.Translate(exception);
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            throw new InventoryConflictException("Transaction conflicted with another operation. Read current state before retrying.", exception);
        }
    }

    public async Task<IReadOnlyList<StockMovement>> ListAsync(Guid? productId, Guid? warehouseId,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.StockMovements.AsNoTracking();
        if (productId.HasValue) query = query.Where(x => x.ProductId == productId.Value);
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId.Value);
        return await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    }
}
