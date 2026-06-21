using Jumbo.ProductCatalog.Core.DTOs;
using Jumbo.ProductCatalog.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Jumbo.ProductCatalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(IProductCatalogService productService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken ct) =>
        Ok(await productService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var product = await productService.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await productService.CreateAsync(request, ct);
        if (!result.IsSuccess)
        {
            return Problem(title: "Validation error", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return CreatedAtAction(nameof(GetByIdAsync), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await productService.UpdateAsync(id, request, ct);
        if (!result.IsSuccess)
        {
            return Problem(title: "Not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await productService.DeleteAsync(id, ct);
        if (!result.IsSuccess)
        {
            return Problem(title: "Not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound);
        }

        return NoContent();
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportAsync([FromBody] List<CreateProductRequest> items, CancellationToken ct) =>
        Ok(await productService.ImportAsync(items, ct));
}
