using Inventory.Api.Contracts;
using Inventory.Application.Contracts;
using Inventory.Application.Features.Warehouses;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
public sealed class WarehousesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WarehouseDto>>> List(
        [FromServices] GetWarehousesUseCase useCase, CancellationToken cancellationToken,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        Ok(await useCase.ExecuteAsync(page, pageSize, cancellationToken));

    [HttpPost]
    [ProducesResponseType<WarehouseDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<WarehouseDto>> Create(CreateWarehouseBody body,
        [FromServices] CreateWarehouseUseCase useCase, CancellationToken cancellationToken)
    {
        var warehouse = await useCase.ExecuteAsync(new(body.Code, body.Name), cancellationToken);
        return Created("/api/warehouses", warehouse);
    }
}
