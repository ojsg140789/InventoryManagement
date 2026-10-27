using Inventory.Application.Abstractions;
using Inventory.Application.Contracts;
using Inventory.Application.Exceptions;
using Inventory.Domain.Entities;

namespace Inventory.Application.Features.Products;

public sealed class CreateProductUseCase(IProductRepository products, IUnitOfWork unitOfWork)
{
    public async Task<ProductDto> ExecuteAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = new Product(request.Sku, request.Name);
        if (await products.SkuExistsAsync(product.Sku, cancellationToken))
            throw new InventoryConflictException("A product with this SKU already exists.");
        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}
