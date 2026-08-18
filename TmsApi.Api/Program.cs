using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly; // M7 Session 4 - Exercise 8: Polly v8 resilience
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;
using TmsApi.Application.Interfaces;
using TmsApi.Behaviors;
using TmsApi.Enrollments.Commands;
using TmsApi.Enrollments.Queries;
using TmsApi.ExceptionHandlers;
using TmsApi.Filters;
using TmsApi.Infrastructure.Caching;     // M7 Session 4 - Exercise 9: TmsMeters.ServiceName
using TmsApi.Infrastructure.ExternalServices; // M7 Session 4 - Exercise 8: CertificateService
using TmsApi.Infrastructure.Persistence;
using TmsApi.Api.Hubs;
using TmsApi.Api.Middleware;
using TmsApi.Api.Legacy;
using TmsApi.Api.RateLimiting;
using TmsApi.Api.Transcripts;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence.Services;
using TmsApi.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// --- M8 Session 3 - Exercise 6: CORS for Angular dev server ---
// The Angular app on localhost:4200 is a different origin from the API on localhost:7190.
// The browser blocks cross-origin requests unless the server explicitly allows them.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// --- M7 Session 4 - Exercise 9: Structured JSON logging with trace correlation ---
// Every log line is JSON with TraceId, SpanId, RequestId, and scope properties.
// The LoggingBehavior from Ex 2 adds RequestName/CorrelationId to scope — these flow through.
// Use: dotnet run 2>&1 | jq 'select(.TraceId == "abc123…")' to grep one trace end-to-end.
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false };
});

// --- M7 Session 4 - Exercise 9: OpenTelemetry traces and metrics ---
// AddMeter name MUST equal TmsMeters.ServiceName ("tms-api") — mismatched strings = silent drop.
// OTLP exporter sends to http://localhost:4317 by default.
// Run Aspire Dashboard (dotnet tool install -g Aspire.Dashboard) or
// Jaeger (docker run --rm -p 16686:16686 -p 4317:4317 jaegertracing/all-in-one) to view.
// If no collector is running, the exporter silently drops spans — the API still starts fine.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: TmsMeters.ServiceName,
        serviceVersion: "1.0.0"))
    .WithTracing(t => t
        .AddSource(TmsMeters.ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddMeter(TmsMeters.ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

// --- M7 Session 4 - Exercise 9: Health checks — liveness vs readiness ---
// Liveness ("live" tag): cheap self-check — tells the orchestrator whether to restart the pod.
// Readiness ("ready" tag): DB check — tells the load balancer to take the pod in/out of rotation.
// A pod that cannot reach its database should leave the pool, not be restarted.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("alive"), tags: ["live"])
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("TmsDatabase")!,
        name: "postgres",
        tags: ["ready"]);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // --- M6 Session 2 - Exercise 4: Global Audit Log Filter ---
    // Cross-cutting concern: logs every API call and its status code.
    // Registered globally so every controller action is covered automatically.
    options.Filters.Add<AuditLogFilter>();
});

// --- Session 3 - Exercise 6: Standardized RFC 9457 Problem Details Service ---
builder.Services.AddProblemDetails();

// --- Session 3 - Exercise 7: Add OpenAPI document services ---
builder.Services.AddOpenApi();

// --- Session 1 - Exercise 1: Registering Authentication and Authorization Services ---
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

// --- Session 2 - Exercise 2: Dependency Injection Registrations ---
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<ILegacyEnrollmentService, LegacyEnrollmentService>();

// --- M7 Session 3 - Exercise 5: Transcript worker and status store ---
builder.Services.AddSingleton<StatusStore>();
builder.Services.AddSingleton(new TranscriptGeneratorOptions());
builder.Services.AddHostedService<TranscriptGenerator>();

// --- M7 Session 3 - Exercise 6: SignalR hub services ---
builder.Services.AddSignalR();

// --- M7 Session 1 - Exercise 1: API Versioning ---
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// --- M7 Session 1 - Exercise 2: MediatR with CQRS ---
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));

// --- M7 Session 1 - Exercise 2: FluentValidation ---
builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

// --- M7 Session 1 - Exercise 2: Pipeline Behaviors (Logging FIRST, Validation SECOND) ---
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// --- M7 Session 1 - Exercise 2: Global Exception Handler ---
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// --- M6 Session 1 - Exercise 1: Register M6 Service Layer ---
// Scoped to match TmsDbContext lifetime — one service instance per HTTP request.
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// --- Session 2 - Exercise 3: strongly-typed Options with Validation ---
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// --- Session 2 - Exercise 2: Active DI Container Validation ---
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// --- M5 Lab Session 1: Register TmsDbContext with PostgreSQL and SQL Logging ---
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
        .LogTo(Console.WriteLine, LogLevel.Information)          // Log SQL to output window
        .EnableSensitiveDataLogging());                          // Show parameters in query logs (dev only)

// --- Session 2 - Exercise 3: HybridCache with stampede protection ---
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});

// --- Session 2 - Exercise 3: Register cached course service ---
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();

// --- M7 Session 4 - Exercise 8: Polly v8 resilience pipeline for certificate service ---
// Order matters: Timeout (outer) → CircuitBreaker (middle) → Retry (inner).
// The retry runs inside the timeout so a single retry attempt is bounded.
// The breaker runs across retries so it sees per-call failures.
builder.Services.AddResiliencePipeline("certificate-api", pipeline =>
{
    pipeline
        // Outer: per-request hard timeout — protects against hung downstream.
        .AddTimeout(TimeSpan.FromSeconds(5))
        // Middle: circuit breaker — protects against sustained outage.
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15),
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>(),
            OnOpened = args =>
            {
                Console.WriteLine("Circuit OPENED — stopping requests to certificate service");
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                Console.WriteLine("Circuit CLOSED — certificate service recovered");
                return ValueTask.CompletedTask;
            }
        })
        // Inner: retry with jitter — only for transient failures.
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>(),
            OnRetry = args =>
            {
                Console.WriteLine(
                    $"Retry #{args.AttemptNumber} after {args.RetryDelay.TotalMilliseconds:F0}ms ({args.Outcome.Exception?.GetType().Name})");
                return ValueTask.CompletedTask;
            }
        });
});

// --- M7 Session 4 - Exercise 8: Register typed HttpClient for CertificateService ---
// BaseAddress points at the running Kestrel URL so the HttpClient can reach /fake/certificates.
builder.Services.AddHttpClient<ICertificateService, CertificateService>((sp, client) =>
{
    var baseUrl = sp.GetRequiredService<IConfiguration>().GetValue<string>("TmsApi:PublicBaseUrl")
        ?? "https://localhost:7190";
    client.BaseAddress = new Uri(baseUrl);
});

// --- Session 2 - Exercise 4: Tier-aware rate limiting ---
builder.Services.AddRateLimiter(options =>
{
    // Global partitioned rate limiter - per API key/client
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);

        return tier switch
        {
            ApiKeyTier.Paid => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"paid:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 200,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            ApiKeyTier.Free => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"free:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 30,
                    TokensPerPeriod = 10,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            _ => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"anon:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 10,
                    TokensPerPeriod = 5,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                })
        };
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts))
            retryAfter = ((int)ts.TotalSeconds).ToString();

        context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Rate limit exceeded",
            Detail = $"Too many requests. Retry after {retryAfter} seconds.",
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://tms.local/errors/rate_limit_exceeded"
        }, ct);
    };

    // Concurrency limiter for transcript endpoint
    options.AddConcurrencyLimiter("transcripts", opt =>
    {
        opt.PermitLimit = 5;
        opt.QueueLimit = 20;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

// --- M7 Session 4 - Exercise 9: Health probe endpoints ---
// /health/live  — liveness: orchestrator restart signal. Never depends on external services.
// /health/ready — readiness: load-balancer in/out-of-rotation signal. Depends on DB.
// Both exempt from rate limiting so they are never throttled under load.
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
}).DisableRateLimiting();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).DisableRateLimiting();

// --- M7 Session 1 - Exercise 1: V1 Deprecation Middleware ---
// Must be registered before MapControllers so every V1 endpoint gets the headers.
app.UseMiddleware<V1DeprecationMiddleware>();

// --- Session 1 - Exercise 1B: Middleware Ordering ---
app.UseMiddleware<RequestLoggingMiddleware>();

// --- M7 Session 1 - Exercise 2: Global Exception Handler ---
app.UseExceptionHandler();
app.UseStatusCodePages();

// --- Session 3 - Exercise 6 & 7: Environment-Aware Error Handling ---
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAngular"); // Must be after UseRouting, before UseAuthentication
app.UseAuthentication();
app.UseAuthorization();

// --- Session 2 - Exercise 4: Rate limiting middleware ---
app.UseRateLimiter();

app.MapControllers();
app.MapHub<TmsHub>("/hub/transcripts");

// --- Session 3 - Exercise 7: Environment-Aware OpenAPI & Scalar Explorer ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// --- Session 1 - Exercise 1: Secured Minimal API Endpoint ---
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
})).RequireAuthorization();

// --- Session 2 - Exercise 2: Enrollment Worker Smoke Test Route ---
app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
{
    worker.ProcessBatch();
    return Results.Ok("processed");
});

// --- Session 3 - Exercise 6: Simulated Error Route ---
app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});

// --- M7 Session 4 - Exercise 8: Lab-only fake certificate service ---
// Simulates three realistic failure modes of an external certificate-printing service:
//   - every 7th call hangs (timeout test)
//   - every 3rd call returns 503 (transient retry test)
//   - every 11th call returns 400 (non-transient, must NOT retry)
// Marked test-only — would never ship to production.
var attempts = 0;
app.MapPost("/fake/certificates", async () =>
{
    var n = Interlocked.Increment(ref attempts);
    if (n % 7 == 0)
    {
        // Hang — simulates a downstream that accepted the request and never responded.
        await Task.Delay(TimeSpan.FromSeconds(20));
        return Results.Ok(new { Status = "issued", Attempt = n });
    }
    if (n % 3 != 0)
    {
        // Transient: 503 Service Unavailable — Polly retries this.
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    if (n % 11 == 0)
    {
        // Non-transient: 400 — Polly must NOT retry this.
        return Results.BadRequest(new { error = "validation_failed" });
    }
    return Results.Ok(new { Status = "issued", Attempt = n });
}).WithTags("lab-fixtures");

// --- M5 Lab Session 1: Auto-Seed test data at startup ---
// Migration and seeding can fail when a remote DB is unreachable (CI/dev). To allow
// frontend development without a live Postgres, respect the SKIP_DB_MIGRATE env var.
var skipMigrate = Environment.GetEnvironmentVariable("SKIP_DB_MIGRATE");
if (!string.Equals(skipMigrate, "true", StringComparison.OrdinalIgnoreCase))
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
        context.Database.Migrate(); // Applies any pending migrations; keeps migration history intact

        if (!context.Students.Any())
        {
            var students = new List<Student>
            {
                new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
                new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
                new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
                new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
                new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
            };
            context.Students.AddRange(students);

            var courses = new List<Course>
            {
                new() { Code = "CS-101", Title = "Introduction to Computer Science", MaxCapacity = 30 },
                new() { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
                new() { Code = "MAT-101", Title = "Calculus I", MaxCapacity = 40 }
            };
            context.Courses.AddRange(courses);

            context.SaveChanges();

            var enrollments = new List<Enrollment>
            {
                new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
                new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
                new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
                new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
            };
            context.Enrollments.AddRange(enrollments);
            context.SaveChanges();
        }
    }
}

// --- M6 Session 2 - Before You Begin: Deterministic Course Seeder ---
// Seeds 25 courses for pagination verification (Development only).
// Idempotent — skips if courses already exist. Skip when SKIP_DB_MIGRATE=true.
if (app.Environment.IsDevelopment() && !string.Equals(skipMigrate, "true", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await DataSeeder.SeedAsync(context);
}

app.Run();
