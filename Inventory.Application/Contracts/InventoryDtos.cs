using Inventory.Domain.Entities;
using Inventory.Domain.Enums;

namespace Inventory.Application.Contracts;

public record CreateProductRequest(string Sku, string Name);
public record CreateWarehouseRequest(string Code, string Name);
public record StockMovementRequest(Guid ProductId, Guid WarehouseId, int Quantity);
public record ProductDto(Guid Id, string Sku, string Name, bool IsActive)
{
    public static ProductDto From(Product value) => new(value.Id, value.Sku, value.Name, value.IsActive);
}
public record WarehouseDto(Guid Id, string Code, string Name, bool IsActive)
{
    public static WarehouseDto From(Warehouse value) => new(value.Id, value.Code, value.Name, value.IsActive);
}
public record StockDto(Guid ProductId, Guid WarehouseId, int Quantity)
{
    public static StockDto From(Stock value) => new(value.ProductId, value.WarehouseId, value.Quantity);
}
public record StockMovementDto(Guid Id, Guid ProductId, Guid WarehouseId,
    StockMovementType Type, int Quantity, DateTimeOffset CreatedAt)
{
    public static StockMovementDto From(StockMovement value) => new(value.Id, value.ProductId,
        value.WarehouseId, value.Type, value.Quantity, value.CreatedAt);
}

public static class InputRules
{
    public static void Id(Guid id, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("Identifier cannot be empty.", name);
    }

    public static void Page(int page, int pageSize)
    {
        if (page < 1 || page > 1_000_000 || pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Page must be between 1 and 1000000; pageSize between 1 and 100.");
    }
}
