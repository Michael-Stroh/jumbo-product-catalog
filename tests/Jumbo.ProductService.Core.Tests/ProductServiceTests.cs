using FluentAssertions;
using Jumbo.ProductService.Core.DTOs;
using Jumbo.ProductService.Core.Interfaces;
using Jumbo.ProductService.Domain.Entities;
using Jumbo.ProductService.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jumbo.ProductService.Core.Tests;

public sealed class ProductServiceTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly Services.ProductService _sut;

    public ProductServiceTests() =>
        _sut = new Services.ProductService(_repository, NullLogger<Services.ProductService>.Instance);

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
        var archived = new Product { Id = 7, Code = "ARC1", Name = "Old", Category = Category.Food, IsActive = false, IsArchived = true };
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
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _sut.UpdateAsync(99, new UpdateProductRequest("Name", Category.Food, null, true));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("99");
    }

    [Fact]
    public async Task UpdateAsync_WhenProductFound_MutatesEntityAndReturnsDto()
    {
        var product = new Product { Code = "P1", Name = "Old Name", Category = Category.Food, IsActive = true };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.UpdateAsync(1, new UpdateProductRequest("New Name", Category.NonFood, "content", false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.Category.Should().Be(Category.NonFood);
        await _repository.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_ReturnsFailure()
    {
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _sut.DeleteAsync(99);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenProductFound_CallsRepositoryDeleteAndSucceeds()
    {
        var product = new Product { Code = "P1", Name = "Name", Category = Category.Food, IsActive = true };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.DeleteAsync(1);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).DeleteAsync(product, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _sut.GetByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedDto()
    {
        var product = new Product { Id = 5, Code = "P5", Name = "Name", Category = Category.Food, IsActive = true };
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _sut.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Code.Should().Be("P5");
    }
}
