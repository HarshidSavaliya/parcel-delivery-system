using Microsoft.AspNetCore.Builder;

namespace ParcelDeliverySystem.Middleware;

/// <summary>
/// Extension methods for registering custom middleware components in the ASP.NET Core pipeline.
/// </summary>
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
