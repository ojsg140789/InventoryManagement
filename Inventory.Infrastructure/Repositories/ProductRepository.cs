using Inventory.Application.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public sealed class ProductRepository(InventoryDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(x => x.Sku == sku, cancellationToken);

    public async Task<IReadOnlyList<Product>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        await context.Products.AsNoTracking().OrderBy(x => x.Sku).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

    public void Add(Product product) => context.Products.Add(product);
}
