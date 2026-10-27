using Inventory.Application.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public sealed class StockRepository(InventoryDbContext context) : IStockRepository
{
    public Task<Stock?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken) =>
        context.Stocks.AsNoTracking().SingleOrDefaultAsync(
            x => x.ProductId == productId && x.WarehouseId == warehouseId, cancellationToken);
}
