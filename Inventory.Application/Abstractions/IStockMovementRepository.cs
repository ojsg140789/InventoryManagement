using Inventory.Domain.Entities;

namespace Inventory.Application.Abstractions;

public interface IStockMovementRepository
{
    // Applies stock and movement atomically; failures must roll back both.
    Task RegisterAsync(StockMovement movement, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockMovement>> ListAsync(Guid? productId, Guid? warehouseId,
        int page, int pageSize, CancellationToken cancellationToken);
}
