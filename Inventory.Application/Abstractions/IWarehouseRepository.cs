using Inventory.Domain.Entities;

namespace Inventory.Application.Abstractions;

public interface IWarehouseRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<Warehouse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    void Add(Warehouse warehouse);
}
