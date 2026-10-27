using Inventory.Application.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public sealed class WarehouseRepository(InventoryDbContext context) : IWarehouseRepository
{
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Warehouses.AnyAsync(x => x.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        context.Warehouses.AnyAsync(x => x.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Warehouse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        await context.Warehouses.AsNoTracking().OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

    public void Add(Warehouse warehouse) => context.Warehouses.Add(warehouse);
}
