using Inventory.Application.Abstractions;
using Inventory.Application.Contracts;
using Inventory.Application.Exceptions;
using Inventory.Domain.Entities;

namespace Inventory.Application.Features.Warehouses;

public sealed class CreateWarehouseUseCase(IWarehouseRepository warehouses, IUnitOfWork unitOfWork)
{
    public async Task<WarehouseDto> ExecuteAsync(CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var warehouse = new Warehouse(request.Code, request.Name);
        if (await warehouses.CodeExistsAsync(warehouse.Code, cancellationToken))
            throw new InventoryConflictException("A warehouse with this code already exists.");
        warehouses.Add(warehouse);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return WarehouseDto.From(warehouse);
    }
}
