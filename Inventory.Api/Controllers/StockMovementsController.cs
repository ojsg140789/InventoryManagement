using Inventory.Api.Contracts;
using Inventory.Application.Contracts;
using Inventory.Application.Features.StockMovements;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/stock-movements")]
public sealed class StockMovementsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>> List(
        [FromServices] GetStockMovementsUseCase useCase, CancellationToken cancellationToken,
        [FromQuery] Guid? productId = null, [FromQuery] Guid? warehouseId = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        Ok(await useCase.ExecuteAsync(productId, warehouseId, page, pageSize, cancellationToken));

    [HttpPost("entries")]
    [ProducesResponseType<StockMovementDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<StockMovementDto>> Entry(StockMovementBody body,
        [FromServices] RegisterStockEntryUseCase useCase, CancellationToken cancellationToken)
    {
        var movement = await useCase.ExecuteAsync(new(body.ProductId, body.WarehouseId, body.Quantity), cancellationToken);
        return Created($"/api/stock-movements?productId={movement.ProductId}&warehouseId={movement.WarehouseId}", movement);
    }

    [HttpPost("outputs")]
    [ProducesResponseType<StockMovementDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<StockMovementDto>> Output(StockMovementBody body,
        [FromServices] RegisterStockOutputUseCase useCase, CancellationToken cancellationToken)
    {
        var movement = await useCase.ExecuteAsync(new(body.ProductId, body.WarehouseId, body.Quantity), cancellationToken);
        return Created($"/api/stock-movements?productId={movement.ProductId}&warehouseId={movement.WarehouseId}", movement);
    }
}
