using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using QuotesApi.Middleware;

namespace Quotes.Tests.Unit;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler =
        new(NullLogger<GlobalExceptionHandler>.Instance);

    private static DefaultHttpContext MakeContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task TryHandleAsync_GenericException_Returns500()
    {
        var ctx = MakeContext();
        var ex = new InvalidOperationException("something broke");

        var handled = await _handler.TryHandleAsync(ctx, ex, CancellationToken.None);

        handled.Should().BeTrue();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task TryHandleAsync_BadHttpRequestException_PreservesStatusCode()
    {
        var ctx = MakeContext();
        var ex = new BadHttpRequestException("bad body", StatusCodes.Status400BadRequest);

        var handled = await _handler.TryHandleAsync(ctx, ex, CancellationToken.None);

        handled.Should().BeTrue();
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task TryHandleAsync_WritesJsonBody()
    {
        var ctx = MakeContext();
        ctx.Response.Body = new System.IO.MemoryStream();
        var ex = new Exception("test error");

        await _handler.TryHandleAsync(ctx, ex, CancellationToken.None);

        ctx.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
        using var reader = new System.IO.StreamReader(ctx.Response.Body);
        var json = await reader.ReadToEndAsync();
        json.Should().Contain("test error");
    }
}
