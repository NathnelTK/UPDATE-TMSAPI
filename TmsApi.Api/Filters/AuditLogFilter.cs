using Microsoft.AspNetCore.Mvc.Filters;

namespace TmsApi.Filters;

/// <summary>
/// Global action filter that logs every API call and its response status code.
/// Cross-cutting concern — lives in a filter, not duplicated across every controller action.
/// Registered globally in Program.cs via options.Filters.Add<AuditLogFilter>().
/// </summary>
public class AuditLogFilter(ILogger<AuditLogFilter> logger) : IActionFilter
{
    /// <summary>
    /// Called before the action method executes.
    /// Logs the HTTP method and route path.
    /// </summary>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var route = context.HttpContext.Request.Path;
        var method = context.HttpContext.Request.Method;

        logger.LogInformation("TMS API call: {Method} {Route}", method, route);
    }

    /// <summary>
    /// Called after the action method executes.
    /// Logs the HTTP response status code.
    /// </summary>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        var status = context.HttpContext.Response.StatusCode;

        logger.LogInformation("TMS API response: {StatusCode}", status);
    }
}