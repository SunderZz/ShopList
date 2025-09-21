using System.Diagnostics;
using System.Net;
using ListeDeCourses.Api.Common;

namespace ListeDeCourses.Api.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            var status = ex.HttpStatus ?? HttpStatusCode.BadRequest;
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

            _logger.LogWarning(ex, "Domain error ({TraceId}): {Message}", traceId, ex.Message);
            await WriteErrorResponse(context, status, ex.Message, ex.Code, traceId);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

            _logger.LogError(ex, "Unhandled error ({TraceId})", traceId);
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "Une erreur interne est survenue.", traceId: traceId);
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message, string? code = null, string? traceId = null)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message, code, traceId);
        await context.Response.WriteAsJsonAsync(response);
    }
}
