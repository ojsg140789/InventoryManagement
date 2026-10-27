using Inventory.Application.Abstractions;
using Inventory.Application.Contracts;
using Inventory.Application.Exceptions;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;

namespace Inventory.Application.Features.StockMovements;

internal static class MovementRegistration
{
    public static async Task<StockMovementDto> ExecuteAsync(StockMovementRequest request, StockMovementType type,
        IProductRepository products, IWarehouseRepository warehouses, IStockMovementRepository movements,
        CancellationToken cancellationToken)
    {
        var movement = new StockMovement(request.ProductId, request.WarehouseId, type, request.Quantity);
        if (await products.GetByIdAsync(request.ProductId, cancellationToken) is null)
            throw new ResourceNotFoundException("Product not found.");
        if (!await warehouses.ExistsAsync(request.WarehouseId, cancellationToken))
            throw new ResourceNotFoundException("Warehouse not found.");
        await movements.RegisterAsync(movement, cancellationToken);
        return StockMovementDto.From(movement);
    }
}
