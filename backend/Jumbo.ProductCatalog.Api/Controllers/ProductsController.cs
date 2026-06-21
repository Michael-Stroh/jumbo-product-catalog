using Jumbo.ProductCatalog.Core.DTOs;
using Jumbo.ProductCatalog.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Jumbo.ProductCatalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(IProductCatalogService productService, IOutputCacheStore cacheStore) : ControllerBase
{
    private const string ProductListTag = "products-list";

    [HttpGet]
    [OutputCache(Duration = 300, Tags = [ProductListTag])]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken ct) =>
        Ok(await productService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var product = await productService.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await productService.CreateAsync(request, ct);
        if (!result.IsSuccess)
        {
            return Problem(title: "Validation error", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        await cacheStore.EvictByTagAsync(ProductListTag, ct);
        return Created($"{Request.Path}/{result.Value.Id}", result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await productService.UpdateAsync(id, request, ct);
        if (!result.IsSuccess)
        {
            return Problem(title: "Not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound);
        }

        await cacheStore.EvictByTagAsync(ProductListTag, ct);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await productService.DeleteAsync(id, ct);
        if (!result.IsSuccess)
        {
            return Problem(title: "Not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound);
        }

        await cacheStore.EvictByTagAsync(ProductListTag, ct);
        return NoContent();
    }

    [HttpPost("import")]
    [ProducesResponseType<ImportResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportAsync([FromBody] List<CreateProductRequest> items, CancellationToken ct)
    {
        var result = await productService.ImportAsync(items, ct);
        await cacheStore.EvictByTagAsync(ProductListTag, ct);
        return Ok(result);
    }
}
