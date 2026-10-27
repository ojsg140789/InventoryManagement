using Inventory.Application.Contracts;
using Inventory.Application.Exceptions;
using Inventory.Application.Features.Stocks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/stocks")]
public sealed class StocksController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StockDto>> Get(
        [FromQuery, BindRequired] Guid productId, [FromQuery, BindRequired] Guid warehouseId,
        [FromServices] GetStockUseCase useCase, CancellationToken cancellationToken)
    {
        var stock = await useCase.ExecuteAsync(productId, warehouseId, cancellationToken)
            ?? throw new ResourceNotFoundException("Stock not found. It is initialized by the first successful entry.");
        return Ok(StockDto.From(stock));
    }
}
