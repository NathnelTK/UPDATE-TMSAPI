using System.Diagnostics;

// --- Session 1 - Exercise 1B: Custom Request Logging Middleware ---
// This middleware intercepts every HTTP request to generate and attach a short
// Correlation ID ("X-Correlation-Id"), tracks the processing duration with a Stopwatch,
// and logs structured entry/exit lines referencing the shared Correlation ID.
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate a short 8-character correlation ID
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        // Attach X-Correlation-Id to the response headers early (before downstream middleware runs)
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // Measure elapsed time using a Stopwatch
        var stopwatch = Stopwatch.StartNew();

        // Entry structured log
        _logger.LogInformation(
            "Request {Method} {Path} started. [CorrelationId: {CorrelationId}]",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        try
        {
            // Pass control down to the next middleware in the request pipeline
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Exit structured log containing method, path, status code, duration, and correlation ID
            _logger.LogInformation(
                "Request {Method} {Path} completed with Status: {StatusCode} in {ElapsedMs}ms. [CorrelationId: {CorrelationId}]",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }
}
