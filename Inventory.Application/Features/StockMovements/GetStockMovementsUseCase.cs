using Inventory.Application.Abstractions;
using Inventory.Application.Contracts;

namespace Inventory.Application.Features.StockMovements;

public sealed class GetStockMovementsUseCase(IStockMovementRepository movements)
{
    public async Task<IReadOnlyList<StockMovementDto>> ExecuteAsync(Guid? productId, Guid? warehouseId,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        InputRules.Page(page, pageSize);
        if (productId.HasValue) InputRules.Id(productId.Value, nameof(productId));
        if (warehouseId.HasValue) InputRules.Id(warehouseId.Value, nameof(warehouseId));
        return (await movements.ListAsync(productId, warehouseId, page, pageSize, cancellationToken))
            .Select(StockMovementDto.From).ToList();
    }
}
