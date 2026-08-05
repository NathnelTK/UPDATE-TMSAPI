using System.Diagnostics.Metrics;

namespace TmsApi.Infrastructure.Caching;

// --- M7 Session 4 - Exercise 9: Custom OpenTelemetry meter for cache observability ---
// The standard ASP.NET instrumentation gives request counts and latency for free.
// This custom meter surfaces a business signal only this team knows:
// the actual hit/miss ratio of the course cache, in real time.
// The meter name "tms-api" MUST match AddMeter("tms-api") in Program.cs.
public static class TmsMeters
{
    // Centralised service name — referenced by both the Meter and AddMeter() registration.
    public const string ServiceName = "tms-api";

    public static readonly Meter Meter = new(ServiceName);

    // Counter for cache hits — rises sharply when the cache is doing its job.
    public static readonly Counter<long> CacheHits =
        Meter.CreateCounter<long>("tms.cache.hits", description: "Course cache hits");

    // Counter for cache misses — should rise gently (1 per cache entry expiry).
    public static readonly Counter<long> CacheMisses =
        Meter.CreateCounter<long>("tms.cache.misses", description: "Course cache misses");
}