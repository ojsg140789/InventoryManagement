using Inventory.Application.Abstractions;
using Inventory.Application.Contracts;
using Inventory.Domain.Enums;

namespace Inventory.Application.Features.StockMovements;

public sealed class RegisterStockEntryUseCase(IProductRepository products, IWarehouseRepository warehouses,
    IStockMovementRepository movements)
{
    public Task<StockMovementDto> ExecuteAsync(StockMovementRequest request, CancellationToken cancellationToken) =>
        MovementRegistration.ExecuteAsync(request, StockMovementType.Entry, products, warehouses, movements, cancellationToken);
}
