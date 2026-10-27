using Inventory.Api.Contracts;
using Inventory.Application.Contracts;
using Inventory.Application.Features.Products;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> List(
        [FromServices] GetProductsUseCase useCase, CancellationToken cancellationToken,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        Ok(await useCase.ExecuteAsync(page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, [FromServices] GetProductByIdUseCase useCase,
        CancellationToken cancellationToken) => Ok(await useCase.ExecuteAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductBody body,
        [FromServices] CreateProductUseCase useCase, CancellationToken cancellationToken)
    {
        var product = await useCase.ExecuteAsync(new(body.Sku, body.Name), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }
}
