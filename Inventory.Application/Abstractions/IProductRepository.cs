using Inventory.Domain.Entities;

namespace Inventory.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    void Add(Product product);
}
