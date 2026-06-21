using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jumbo.ProductCatalog.Core.Interfaces;
using Jumbo.ProductCatalog.Core.Services;
using Jumbo.ProductCatalog.Domain.Entities;
using Jumbo.ProductCatalog.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Jumbo.ProductCatalog.Tests.UnitTests.Core.Services;

[Trait("Category", "Unit")]
public sealed class ExportServiceTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IFileStorageService _storageWriter = Substitute.For<IFileStorageService>();
    private readonly ExportService _sut;

    public ExportServiceTests() =>
        _sut = new ExportService(_repository, _storageWriter, NullLogger<ExportService>.Instance);

    [Fact]
    public async Task ExportActiveProductsAsync_WhenNoActiveProducts_WritesEmptyArrayAndReturnsZeroCount()
    {
        _repository.GetActiveAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.ExportActiveProductsAsync();

        result.ExportedCount.Should().Be(0);
        result.FileName.Should().StartWith("active-products-").And.EndWith(".json");
        await _storageWriter.Received(1).WriteAsync(
            Arg.Is<string>(n => n.StartsWith("active-products-") && n.EndsWith(".json")),
            Arg.Any<byte[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExportActiveProductsAsync_WithActiveProducts_ReturnsCorrectCountAndUploadsOnce()
    {
        var products = (IReadOnlyList<Product>)
        [
            new Product { Code = "A1", Name = "Alpha", Category = Category.Food, IsActive = true },
            new Product { Code = "B2", Name = "Beta",  Category = Category.NonFood, IsActive = true },
        ];
        _repository.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(products);

        var result = await _sut.ExportActiveProductsAsync();

        result.ExportedCount.Should().Be(2);
        result.FileName.Should().StartWith("active-products-").And.EndWith(".json");
        await _storageWriter.Received(1).WriteAsync(
            result.FileName,
            Arg.Is<byte[]>(b => b.Length > 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExportActiveProductsAsync_FileNameMatchesTimestampPattern()
    {
        _repository.GetActiveAsync(Arg.Any<CancellationToken>()).Returns([]);
        string capturedFileName = string.Empty;
        _storageWriter
            .When(s => s.WriteAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>()))
            .Do(call => capturedFileName = call.ArgAt<string>(0));

        await _sut.ExportActiveProductsAsync();

        Regex.IsMatch(capturedFileName, @"^active-products-\d{14}\.json$").Should().BeTrue(
            "filename should match active-products-yyyyMMddHHmmss.json");
    }

    [Fact]
    public async Task ExportActiveProductsAsync_SerializesAllProductFields()
    {
        var product = new Product
        {
            Code = "TEST1",
            Name = "Test Product",
            Category = Category.Food,
            Content = "some content",
            IsActive = true,
        };
        _repository.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<Product>)[product]);
        byte[] capturedBytes = [];
        _storageWriter
            .When(s => s.WriteAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>()))
            .Do(call => capturedBytes = call.ArgAt<byte[]>(1));

        await _sut.ExportActiveProductsAsync();

        var json = JsonSerializer.Deserialize<JsonElement[]>(capturedBytes)!;
        var first = json.Single();
        first.GetProperty("Code").GetString().Should().Be("TEST1");
        first.GetProperty("Name").GetString().Should().Be("Test Product");
        first.GetProperty("Category").GetString().Should().Be("Food");
        first.GetProperty("Content").GetString().Should().Be("some content");
        first.GetProperty("IsActive").GetBoolean().Should().BeTrue();
    }
}
