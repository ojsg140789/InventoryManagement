using Inventory.Application.Abstractions;
using Inventory.Application.Contracts;

namespace Inventory.Application.Features.Products;

public sealed class GetProductsUseCase(IProductRepository products)
{
    public async Task<IReadOnlyList<ProductDto>> ExecuteAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        InputRules.Page(page, pageSize);
        return (await products.ListAsync(page, pageSize, cancellationToken)).Select(ProductDto.From).ToList();
    }
}
