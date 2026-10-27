using System.ComponentModel.DataAnnotations;

namespace Inventory.Api.Contracts;

public sealed record CreateProductBody([Required] string Sku, [Required] string Name);
public sealed record CreateWarehouseBody([Required] string Code, [Required] string Name);
public sealed record StockMovementBody(Guid ProductId, Guid WarehouseId, [Range(1, int.MaxValue)] int Quantity);
