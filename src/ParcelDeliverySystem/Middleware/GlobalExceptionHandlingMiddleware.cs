using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ParcelDeliverySystem.Middleware;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions,
/// logs diagnostics via ILogger, and provides friendly, secure error responses
/// without exposing stack traces or database connection details.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Handle HTTP error status codes (e.g. 404, 403, 401)
            if (!context.Response.HasStarted)
            {
                if (context.Response.StatusCode == (int)HttpStatusCode.NotFound)
                {
                    await HandleStatusCodeAsync(context, 404, "The requested resource could not be found.");
                }
                else if (context.Response.StatusCode == (int)HttpStatusCode.Forbidden)
                {
                    context.Response.Redirect("/Account/AccessDenied");
                }
                else if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
                {
                    context.Response.Redirect("/Account/Login");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception occurred while processing request: {Method} {Path} by User: {User}",
                context.Request.Method,
                context.Request.Path,
                context.User.Identity?.Name ?? "Anonymous"
            );

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var isJsonRequest = context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                            context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                            context.Request.Path.StartsWithSegments("/api");

        if (isJsonRequest)
        {
            context.Response.ContentType = "application/json";
            var responseObj = new
            {
                success = false,
                statusCode = 500,
                message = "An unexpected error occurred while processing your request. Please try again later.",
                requestId = context.TraceIdentifier
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(responseObj));
        }
        else
        {
            context.Response.Redirect($"/Home/Error?statusCode=500&requestId={context.TraceIdentifier}");
        }
    }

    private static async Task HandleStatusCodeAsync(HttpContext context, int statusCode, string message)
    {
        var isJsonRequest = context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                            context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                            context.Request.Path.StartsWithSegments("/api");

        if (isJsonRequest)
        {
            context.Response.ContentType = "application/json";
            var responseObj = new
            {
                success = false,
                statusCode,
                message,
                requestId = context.TraceIdentifier
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(responseObj));
        }
        else
        {
            context.Response.Redirect($"/Home/Error?statusCode={statusCode}");
        }
    }
}
