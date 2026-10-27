using Inventory.Application.Abstractions;
using Inventory.Application.Contracts;

namespace Inventory.Application.Features.Warehouses;

public sealed class GetWarehousesUseCase(IWarehouseRepository warehouses)
{
    public async Task<IReadOnlyList<WarehouseDto>> ExecuteAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        InputRules.Page(page, pageSize);
        return (await warehouses.ListAsync(page, pageSize, cancellationToken)).Select(WarehouseDto.From).ToList();
    }
}
