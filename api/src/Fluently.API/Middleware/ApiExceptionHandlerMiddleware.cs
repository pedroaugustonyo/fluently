using System.Diagnostics;
using System.Text.Json;
using Fluently.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Fluently.API.Middleware;

/// <summary>
/// Tratamento padronizado das exceções da aplicação.
/// </summary>
public sealed class ApiExceptionHandlerMiddleware(ILogger<ApiExceptionHandlerMiddleware> logger) : IExceptionHandler
{
    /// <summary>
    /// Trata uma exceção e escreve os detalhes padronizados na resposta HTTP.
    /// </summary>
    /// <param name="httpContext">Contexto da requisição atual.</param>
    /// <param name="exception">Exceção que será tratada.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Valor que indica se a exceção foi tratada.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var (statusCode, detail) = MapException(exception);
        var title = ReasonPhrases.GetReasonPhrase(statusCode);
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.Message);
        Activity.Current?.SetTag("error.type", exception.GetType().FullName);
        Activity.Current?.SetTag("error.status_code", statusCode);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled API exception. {ExceptionType} {StatusCode} {RequestMethod} {RequestPath} {TraceId}",
                exception.GetType().FullName,
                statusCode,
                httpContext.Request.Method,
                httpContext.Request.Path,
                traceId
            );
        }
        else
        {
            logger.LogWarning(
                "API request rejected. {ExceptionType} {StatusCode} {RequestMethod} {RequestPath} {TraceId}",
                exception.GetType().FullName,
                statusCode,
                httpContext.Request.Method,
                httpContext.Request.Path,
                traceId
            );
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };
        problemDetails.Extensions["traceId"] = traceId;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken
        );

        return true;
    }

    /// <summary>
    /// Converte uma exceção para os dados públicos da resposta HTTP.
    /// </summary>
    /// <param name="exception">Exceção que será convertida.</param>
    /// <returns>Código HTTP e detalhe público do erro.</returns>
    private static (int StatusCode, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            ApiException apiException => (apiException.StatusCode, apiException.Detail),
            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "Não foi possível salvar os dados porque eles entram em conflito com o estado atual."
            ),
            HttpRequestException or JsonException or TimeoutException or OperationCanceledException => (
                StatusCodes.Status503ServiceUnavailable,
                "Não foi possível acessar o serviço de inteligência artificial agora."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Ocorreu um erro inesperado. Entre em contato com o suporte e informe o traceId."
            ),
        };
    }
}
