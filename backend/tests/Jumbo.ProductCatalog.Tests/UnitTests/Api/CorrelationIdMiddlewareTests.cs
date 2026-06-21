using FluentAssertions;
using Jumbo.ProductCatalog.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace Jumbo.ProductCatalog.Tests.UnitTests.Api;

[Trait("Category", "Unit")]
public sealed class CorrelationIdMiddlewareTests
{
    private const string HeaderName = "X-Correlation-ID";

    private static DefaultHttpContext CreateContext(string? correlationId = null)
    {
        var context = new DefaultHttpContext();
        if (correlationId is not null)
        {
            context.Request.Headers[HeaderName] = correlationId;
        }

        return context;
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderPresent_EchoesCorrelationIdOnResponse()
    {
        const string correlationId = "abc-123-xyz";
        var context = CreateContext(correlationId);
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Items[HeaderName].Should().Be(correlationId);
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderAbsent_GeneratesAndEchoesCorrelationId()
    {
        var context = CreateContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var id = context.Items[HeaderName].Should().BeOfType<string>().Subject;
        id.Should().NotBeNullOrEmpty();
    }
}
