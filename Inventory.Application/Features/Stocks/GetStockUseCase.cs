using Inventory.Application.Abstractions;
using Inventory.Domain.Entities;

namespace Inventory.Application.Features.Stocks
{
    public class GetStockUseCase
    {
        private readonly IStockRepository _stockRepository;
        public GetStockUseCase(IStockRepository stockRepository)
        {
            _stockRepository = stockRepository;
        }

        public async Task<Stock?> ExecuteAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken)
        {
            Inventory.Application.Contracts.InputRules.Id(productId, nameof(productId));
            Inventory.Application.Contracts.InputRules.Id(warehouseId, nameof(warehouseId));
            return await _stockRepository.GetByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);
        }
    }
}
