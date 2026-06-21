using FluentAssertions;
using Jumbo.ProductCatalog.Api.Controllers;
using Jumbo.ProductCatalog.Core.DTOs;
using Jumbo.ProductCatalog.Core.Interfaces;
using Jumbo.ProductCatalog.Domain.Common;
using Jumbo.ProductCatalog.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using NSubstitute;

namespace Jumbo.ProductCatalog.Tests.UnitTests.Api.Controllers;

[Trait("Category", "Unit")]
public sealed class ProductsControllerTests
{
    private readonly IProductCatalogService _service = Substitute.For<IProductCatalogService>();
    private readonly IOutputCacheStore _cacheStore = Substitute.For<IOutputCacheStore>();
    private readonly ProductsController _sut;

    public ProductsControllerTests()
    {
        _sut = new ProductsController(_service, _cacheStore)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { Request = { Path = "/api/products" } },
            },
        };
    }

    private static ProductDto MakeProduct(Guid? id = null) => new(
        id ?? Guid.NewGuid(), "SKU1", "Product 1", Category.Food, null, true,
        DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public async Task GetAllAsync_ReturnsOkWithList()
    {
        var products = (IReadOnlyList<ProductDto>)[MakeProduct(), MakeProduct()];
        _service.GetAllAsync(Arg.Any<CancellationToken>()).Returns(products);

        var response = await _sut.GetAllAsync(CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(products);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_Returns200WithProduct()
    {
        var product = MakeProduct();
        _service.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var response = await _sut.GetByIdAsync(product.Id, CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(product);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_Returns404()
    {
        _service.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ProductDto?)null);

        var response = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        response.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateAsync_Success_Returns201WithProduct()
    {
        var product = MakeProduct();
        var request = new CreateProductRequest("SKU1", "Product 1", Category.Food, null);
        _service.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(Result.Success(product));

        var response = await _sut.CreateAsync(request, CancellationToken.None);

        var created = response.Should().BeOfType<CreatedResult>().Subject;
        created.Value.Should().Be(product);
        created.Location.Should().Be($"/api/products/{product.Id}");
    }

    [Fact]
    public async Task CreateAsync_Failure_Returns400()
    {
        var request = new CreateProductRequest("SKU1", "Product 1", Category.Food, null);
        _service.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(Result.Failure<ProductDto>("Duplicate code"));

        var response = await _sut.CreateAsync(request, CancellationToken.None);

        var problem = response.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateAsync_Success_Returns200WithProduct()
    {
        var product = MakeProduct();
        var request = new UpdateProductRequest("Product 1 Updated", Category.Food, null, true);
        _service.UpdateAsync(product.Id, request, Arg.Any<CancellationToken>()).Returns(Result.Success(product));

        var response = await _sut.UpdateAsync(product.Id, request, CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(product);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var request = new UpdateProductRequest("Name", Category.Food, null, true);
        _service.UpdateAsync(id, request, Arg.Any<CancellationToken>()).Returns(Result.Failure<ProductDto>("Not found"));

        var response = await _sut.UpdateAsync(id, request, CancellationToken.None);

        var problem = response.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAsync_Success_Returns204()
    {
        var id = Guid.NewGuid();
        _service.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var response = await _sut.DeleteAsync(id, CancellationToken.None);

        response.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteAsync_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _service.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(Result.Failure("Not found"));

        var response = await _sut.DeleteAsync(id, CancellationToken.None);

        var problem = response.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ImportAsync_ValidBatch_Returns200WithImportResult()
    {
        var expected = new ImportResult(2, []);
        _service.ImportAsync(Arg.Any<IReadOnlyList<CreateProductRequest>>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var items = new List<CreateProductRequest>
        {
            new("I1", "Item 1", Category.Food, null),
            new("I2", "Item 2", Category.NonFood, null),
        };

        var response = await _sut.ImportAsync(items, CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(expected);
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_Returns200WithEmptyList()
    {
        _service.GetAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<ProductDto>)[]);

        var response = await _sut.GetAllAsync(CancellationToken.None);

        var ok = response.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<ProductDto>>()
            .Which.Should().BeEmpty();
    }
}
