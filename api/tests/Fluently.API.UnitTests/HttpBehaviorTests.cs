using System.Text.Json;

using Fluently.API.Exceptions;
using Fluently.API.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fluently.API.UnitTests;

public sealed class HttpBehaviorTests
{
    [Fact]
    public async Task ExceptionHandler_KnownApiException_WritesEnglishStatusTitle()
    {
        var context = CreateContext();
        context.Request.Path = "/api/v1/questions";
        context.Response.Body = new MemoryStream();
        var handler = new ApiExceptionHandlerMiddleware(
            NullLogger<ApiExceptionHandlerMiddleware>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new ConflictException("Esta questão já foi respondida."),
            CancellationToken.None);
        var problem = await ReadResponseAsync(context);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal("Conflict", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "Esta questão já foi respondida.",
            problem.RootElement.GetProperty("detail").GetString());
        Assert.Equal(
            context.TraceIdentifier,
            problem.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task ExceptionHandler_UnexpectedException_WritesEnglishStatusTitleWithoutLeak()
    {
        var context = CreateContext();
        context.Request.Path = "/api/v1/questions";
        context.Response.Body = new MemoryStream();
        var handler = new ApiExceptionHandlerMiddleware(
            NullLogger<ApiExceptionHandlerMiddleware>.Instance);

        await handler.TryHandleAsync(
            context,
            new InvalidOperationException("sensitive internal detail"),
            CancellationToken.None);
        var problem = await ReadResponseAsync(context);
        var body = problem.RootElement.GetRawText();

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("Internal Server Error", problem.RootElement.GetProperty("title").GetString());
        Assert.Contains("Ocorreu um erro inesperado", body);
        Assert.DoesNotContain("sensitive internal detail", body);
        Assert.Equal(
            context.TraceIdentifier,
            problem.RootElement.GetProperty("traceId").GetString());
    }

    private static DefaultHttpContext CreateContext()
    {
        return new DefaultHttpContext();
    }

    private static async Task<JsonDocument> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;

        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
