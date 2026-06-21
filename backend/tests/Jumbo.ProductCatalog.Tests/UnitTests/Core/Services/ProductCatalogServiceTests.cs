using FluentAssertions;
using Jumbo.ProductCatalog.Core.DTOs;
using Jumbo.ProductCatalog.Core.Interfaces;
using Jumbo.ProductCatalog.Core.Services;
using Jumbo.ProductCatalog.Domain.Entities;
using Jumbo.ProductCatalog.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jumbo.ProductCatalog.Tests.UnitTests.Core.Services;

[Trait("Category", "Unit")]
public sealed class ProductCatalogServiceTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly ProductCatalogService _sut;

    public ProductCatalogServiceTests() =>
        _sut = new ProductCatalogService(_repository, NullLogger<ProductCatalogService>.Instance);

    [Fact]
    public async Task CreateAsync_WhenCodeAlreadyExists_ReturnsFailure()
    {
        var active = new Product { Code = "CODE1", Name = "Existing", Category = Category.Food, IsActive = true };
        _repository.GetByCodeIncludingArchivedAsync("CODE1", Arg.Any<CancellationToken>()).Returns(active);

        var result = await _sut.CreateAsync(new CreateProductRequest("CODE1", "Name", Category.Food, null));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("CODE1");
    }

    [Fact]
    public async Task CreateAsync_WhenCodeIsNew_ReturnsSuccessWithMappedDto()
    {
        _repository.GetByCodeIncludingArchivedAsync("NEW1", Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _sut.CreateAsync(new CreateProductRequest("NEW1", "Product Name", Category.NonFood, "content"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("NEW1");
        result.Value.Name.Should().Be("Product Name");
        await _repository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenCodeMatchesArchivedProduct_ReactivatesAndReturnsDto()
    {
        var archived = new Product { Id = Guid.NewGuid(), Code = "ARC1", Name = "Old", Category = Category.Food, IsActive = false, IsArchived = true };
        _repository.GetByCodeIncludingArchivedAsync("ARC1", Arg.Any<CancellationToken>()).Returns(archived);

        var result = await _sut.CreateAsync(new CreateProductRequest("ARC1", "Reborn", Category.NonFood, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("ARC1");
        result.Value.Name.Should().Be("Reborn");
        archived.IsArchived.Should().BeFalse();
        await _repository.Received(1).UpdateAsync(archived, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenProductNotFound_ReturnsFailure()
    {
        var missingId = Guid.NewGuid();
        _repository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _sut.UpdateAsync(missingId, new UpdateProductRequest("Name", Category.Food, null, true));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(missingId.ToString());
    }

    [Fact]
    public async Task UpdateAsync_WhenProductFound_MutatesEntityAndReturnsDto()
    {
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Code = "P1", Name = "Old Name", Category = Category.Food, IsActive = true };
        _repository.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.UpdateAsync(productId, new UpdateProductRequest("New Name", Category.NonFood, "content", false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.Category.Should().Be(Category.NonFood);
        await _repository.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_ReturnsFailure()
    {
        var missingId = Guid.NewGuid();
        _repository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _sut.DeleteAsync(missingId);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenProductFound_CallsRepositoryDeleteAndSucceeds()
    {
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Code = "P1", Name = "Name", Category = Category.Food, IsActive = true };
        _repository.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.DeleteAsync(productId);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).DeleteAsync(product, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var missingId = Guid.NewGuid();
        _repository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _sut.GetByIdAsync(missingId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedDto()
    {
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Code = "P5", Name = "Name", Category = Category.Food, IsActive = true };
        _repository.GetByIdAsync(productId, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.GetByIdAsync(productId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Code.Should().Be("P5");
    }

    [Fact]
    public async Task ImportAsync_AllNew_ProcessesAll()
    {
        _repository.GetByCodesIncludingArchivedAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        var items = new List<CreateProductRequest>
        {
            new("I1", "Item 1", Category.Food, null),
            new("I2", "Item 2", Category.NonFood, null),
            new("I3", "Item 3", Category.Food, null),
        };

        var result = await _sut.ImportAsync(items);

        result.Processed.Should().Be(3);
        result.SkippedCodes.Should().BeEmpty();
        await _repository.Received(1).ImportBatchAsync(
            Arg.Is<IEnumerable<Product>>(p => p.Count() == 3),
            Arg.Is<IEnumerable<Product>>(p => !p.Any()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_WithDuplicateActiveCode_SkipsIt()
    {
        var active = new Product { Code = "DUP1", Name = "Existing", Category = Category.Food, IsActive = true };
        _repository.GetByCodesIncludingArchivedAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { active });

        var result = await _sut.ImportAsync([new("DUP1", "Name", Category.Food, null)]);

        result.Processed.Should().Be(0);
        result.SkippedCodes.Should().ContainSingle().Which.Should().Be("DUP1");
        await _repository.DidNotReceive().ImportBatchAsync(
            Arg.Any<IEnumerable<Product>>(),
            Arg.Any<IEnumerable<Product>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_MixedBatch_CorrectCounts()
    {
        var active = new Product { Code = "DUP1", Name = "Existing", Category = Category.Food, IsActive = true };
        _repository.GetByCodesIncludingArchivedAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { active });

        var items = new List<CreateProductRequest>
        {
            new("NEW1", "Item 1", Category.Food, null),
            new("DUP1", "Item 2", Category.Food, null),
            new("NEW2", "Item 3", Category.NonFood, null),
        };

        var result = await _sut.ImportAsync(items);

        result.Processed.Should().Be(2);
        result.SkippedCodes.Should().ContainSingle().Which.Should().Be("DUP1");
        await _repository.Received(1).ImportBatchAsync(
            Arg.Is<IEnumerable<Product>>(p => p.Count() == 2),
            Arg.Is<IEnumerable<Product>>(p => !p.Any()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_WithArchivedCode_ReactivatesIt()
    {
        var archived = new Product { Code = "ARC1", Name = "Old", Category = Category.Food, IsArchived = true };
        _repository.GetByCodesIncludingArchivedAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { archived });

        var result = await _sut.ImportAsync([new("ARC1", "New Name", Category.NonFood, null)]);

        result.Processed.Should().Be(1);
        result.SkippedCodes.Should().BeEmpty();
        archived.IsArchived.Should().BeFalse();
        archived.Name.Should().Be("New Name");
        archived.Category.Should().Be(Category.NonFood);
        await _repository.Received(1).ImportBatchAsync(
            Arg.Is<IEnumerable<Product>>(p => !p.Any()),
            Arg.Is<IEnumerable<Product>>(p => p.Count() == 1),
            Arg.Any<CancellationToken>());
    }
}
