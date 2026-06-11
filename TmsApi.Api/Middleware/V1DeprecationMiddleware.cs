namespace TmsApi.Api.Middleware;

/// <summary>
/// Middleware that stamps every V1 response with deprecation headers:
/// - Deprecation: true GÇö V1 is officially deprecated
/// - Sunset: RFC 7231 date GÇö when V1 stops responding
/// - Link: rel="successor-version" GÇö where clients should migrate
/// Uses Response.OnStarting so headers are added at the latest possible point
/// (after the controller has determined the response, before the body is flushed).
/// </summary>
public class V1DeprecationMiddleware(RequestDelegate next)
{
    private static readonly DateTimeOffset SunsetDate =
        new(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            if (context.Request.Path.StartsWithSegments("/api/v1"))
            {
                context.Response.Headers["Deprecation"] = "true";
                context.Response.Headers["Sunset"] = SunsetDate.ToString("R");
                context.Response.Headers["Link"] =
                    $"<{context.Request.Scheme}://{context.Request.Host}/api/v2{context.Request.Path.Value?[7..]}>; rel=\"successor-version\"";
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
