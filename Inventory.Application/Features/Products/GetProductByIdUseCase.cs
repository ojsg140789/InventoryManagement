using Inventory.Application.Abstractions;
using Inventory.Application.Contracts;
using Inventory.Application.Exceptions;

namespace Inventory.Application.Features.Products;

public sealed class GetProductByIdUseCase(IProductRepository products)
{
    public async Task<ProductDto> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        InputRules.Id(id, nameof(id));
        var product = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("Product not found.");
        return ProductDto.From(product);
    }
}
