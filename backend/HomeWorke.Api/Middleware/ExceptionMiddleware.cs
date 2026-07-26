using System.Net;
using System.Text.Json;
using HomeWorke.Api.Services;

namespace HomeWorke.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (TimeServiceException ex)
        {
            _logger.LogWarning(ex, "Time service failure");
            await WriteErrorResponse(context, HttpStatusCode.ServiceUnavailable,
                "Time service unavailable", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation");
            await WriteErrorResponse(context, HttpStatusCode.Conflict,
                "Operation rejected", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            await WriteErrorResponse(context, HttpStatusCode.Forbidden,
                "Access denied", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await WriteErrorResponse(context, HttpStatusCode.NotFound,
                "Not found", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError,
                "An unexpected error occurred", ex.Message);
        }
    }

    private static async Task WriteErrorResponse(
        HttpContext context, HttpStatusCode status, string message, string? detail)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var response = new { error = message, detail };
        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
