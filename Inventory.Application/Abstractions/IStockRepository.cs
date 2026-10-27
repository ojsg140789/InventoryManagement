using Inventory.Domain.Entities;

namespace Inventory.Application.Abstractions
{
    public interface IStockRepository
    {
        Task<Stock?> GetByProductAndWarehouseAsync(
            Guid productId,
            Guid warehouseId,
            CancellationToken cancellationToken);
    }
}
