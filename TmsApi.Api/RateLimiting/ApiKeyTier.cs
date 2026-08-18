namespace TmsApi.Api.RateLimiting;

/// <summary>
/// API key tier levels for rate limiting.
/// Paid partners get higher limits than free users, who get higher limits than anonymous.
/// </summary>
public enum ApiKeyTier
{
    Anonymous,
    Free,
    Paid
}

/// <summary>
/// Resolves API key and tier from HTTP context.
/// In production, this would be replaced with JWT-based authentication.
/// </summary>
public static class ApiKeyResolver
{
    private static readonly Dictionary<string, ApiKeyTier> Keys = new(StringComparer.Ordinal)
    {
        ["tms-free-demo-001"] = ApiKeyTier.Free,
        ["tms-paid-001"] = ApiKeyTier.Paid
    };

    public static (string PartitionKey, ApiKeyTier Tier) Resolve(HttpContext ctx)
    {
        var key = ctx.Request.Headers["X-Api-Key"].ToString();
        if (string.IsNullOrEmpty(key))
            return (ctx.Connection.RemoteIpAddress?.ToString() ?? "anonymous", ApiKeyTier.Anonymous);

        return Keys.TryGetValue(key, out var tier)
            ? (key, tier)
            : (key, ApiKeyTier.Anonymous);
    }
}