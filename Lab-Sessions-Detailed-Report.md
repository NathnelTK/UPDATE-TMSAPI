# Training Management System (TMS) API Backend Foundation
## Lab Sessions Detailed Technical Report & Presentation Guide

---

## 🎓 Academic Profile & Project Context
* **Project Name:** Training Management System (TMS) API
* **Course:** Module 4: ASP.NET Core 10.x Fundamentals (Backend Foundation Sprint)
* **Objective:** Building a robust, high-performance, and secure Web API utilizing ASP.NET Core 10, adhering strictly to enterprise-grade software patterns. This report serves as a detailed academic guide to explain the architectural challenges faced during implementation, how they were resolved, and the deep engineering principles behind each solution.

---

## 📋 Executive Summary
Over the course of three progressive lab sessions, we transformed a basic, fragile Web API into a production-ready microservice foundation. The journey highlights the resolution of the **"silent-failure topics"** of ASP.NET Core—issues that compile successfully and run fine with a single local developer, but catastrophically fail under production loads at 3 AM.

This report is organized into three core blocks matching our developmental phases:
1. **Session 1: Request Flow & Visibility** (Middleware pipeline correctness and request correlation).
2. **Session 2: Services Done Right** (Dependency Injection lifetime discipline, configuration safety, and searchable structured logging).
3. **Session 3: API Surface & Production Readiness** (RESTful CRUD controllers, standardized RFC 9457 error handling, and environment-aware security toggles).

---

## 🗺️ Architectural Diagram of the Request Pipeline

The following flowchart illustrates the exact sequence of middleware execution inside our completed application:

```
                  ┌────────────────────────────────────────┐
                  │           Incoming HTTP Request        │
                  └───────────────────┬────────────────────┘
                                      │
                                      ▼
                  ┌────────────────────────────────────────┐
                  │ 1. RequestLoggingMiddleware            │
                  │    - Generates & assigns X-Correlation│
                  │    - Starts Stopwatch                  │
                  └───────────────────┬────────────────────┘
                                      │
                                      ▼
                  ┌────────────────────────────────────────┐
                  │ 2. UseExceptionHandler                 │
                  │    - Catches downstream exceptions     │
                  │    - Outputs RFC 9457 JSON             │
                  └───────────────────┬────────────────────┘
                                      │
                                      ▼
                  ┌────────────────────────────────────────┐
                  │ 3. UseStatusCodePages                  │
                  │    - Converts empty error codes        │
                  │      to RFC 9457 JSON                  │
                  └───────────────────┬────────────────────┘
                                      │
                                      ▼
                  ┌────────────────────────────────────────┐
                  │ 4. UseHttpsRedirection & UseRouting    │
                  └───────────────────┬────────────────────┘
                                      │
                                      ▼
                  ┌────────────────────────────────────────┐
                  │ 5. UseAuthentication & UseAuthorization│
                  └───────────────────┬────────────────────┘
                                      │
                                      ▼
                  ┌────────────────────────────────────────┐
                  │ 6. Endpoints / Controllers             │
                  │    - EnrollmentsController             │
                  │    - Scalar API Reference (Dev only)   │
                  └───────────────────┬────────────────────┘
                                      │ [Downstream execution complete]
                                      ▼
                  ┌────────────────────────────────────────┐
                  │ RequestLoggingMiddleware (Finally Block)│
                  │    - Stops Stopwatch                   │
                  │    - Emits Exit Log with duration & ID │
                  └───────────────────┬────────────────────┘
                                      │
                                      ▼
                  ┌────────────────────────────────────────┐
                  │           Outgoing HTTP Response       │
                  │    (Contains X-Correlation-Id Header)  │
                  └────────────────────────────────────────┘
```

---

# 🚪 Session 1: Request Flow & Visibility

This session establishes request flow discipline, route protection, and request observability.

## 🛠️ Exercise 1: Secured Assessment Endpoint (Middleware Ordering)

### 🔴 The Problem Faced
The application was starting up successfully, but a critical security flaw was detected: the confidential endpoint `GET /api/assessments/results` was returning placeholder JSON to anonymous callers, completely ignoring authentication. 

The original pipeline was registered as follows:
```csharp
app.UseRouting();
app.MapGet("/api/assessments/results", () => ...);
app.UseAuthentication();
app.UseAuthorization();
```
* **Why did it fail?** Middleware components execute in the exact order they are registered in `Program.cs`. Because the endpoint was mapped and executed *before* `UseAuthentication` and `UseAuthorization` were evaluated, anonymous requests hit the terminal endpoint and bypassed all security validation.

### 🟢 How We Fixed It
1. **Wired a Custom Lightweight Authentication Handler (`TrainingAuthHandler`)**:
   We created a temporary authentication handler to challenge anonymous calls and treat requests containing an `X-Training-User` header as authenticated.
2. **Corrected Request Pipeline Sequence**:
   We placed `UseRouting`, then `UseAuthentication`, and then `UseAuthorization` before mapping any controllers or endpoints.
3. **Applied Authorization Requirements**:
   We chained `.RequireAuthorization()` onto the minimal API route to actively trigger the security middleware.

### 💻 Code Highlight: Securing the Endpoint
In `TmsApi/Program.cs`:
```csharp
// 1. Register authentication services during builder startup
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

var app = builder.Build();

// 2. Configure request pipeline in precise chronological order
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 3. Map endpoint requiring authorization
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
})).RequireAuthorization();
```

In `TmsApi/TrainingAuthHandler.cs`:
```csharp
public class TrainingAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TrainingAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Enforce the presence of custom security header
        if (!Request.Headers.ContainsKey("X-Training-User"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing training user header."));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, Request.Headers["X-Training-User"]!) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

---

## 🛠️ Exercise 1B: Custom Request Logging Middleware & X-Correlation-Id

### 🔴 The Problem Faced
When high volumes of concurrent requests hit the server, individual logs became a chaotic mess of unrelated lines. It was impossible to correlate an error log with the specific incoming request that triggered it. Additionally, if an error occurred downstream, exit logs were completely bypassed, leaving request execution times and response statuses unobserved.

### 🟢 How We Fixed It
1. **Developed Custom `RequestLoggingMiddleware`**:
   Designed an interception layer to manage the full request-response lifecycle.
2. **Generated a Unique Correlation Identifier (`X-Correlation-Id`)**:
   Upon request entry, a short 8-character unique identifier is generated from a Guid (`Guid.NewGuid().ToString("N")[..8]`) and attached early to the outgoing response header.
3. **Structured Entry/Exit Telemetry**:
   Utilized `Stopwatch` to track the exact millisecond duration of downstream processing.
4. **Guaranteed Execution with a Try-Finally Block**:
   Wrapped downstream execution (`await _next(context)`) inside a `try-finally` block to guarantee that exit logging executes and records the exact processing duration even if a downstream exception crashes the request.

### 💻 Code Highlight: The Logging Middleware
In `TmsApi/RequestLoggingMiddleware.cs`:
```csharp
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

        // Attach X-Correlation-Id to response headers early
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        // Entry structured log
        _logger.LogInformation(
            "Request {Method} {Path} started. [CorrelationId: {CorrelationId}]",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        try
        {
            await _next(context); // Pass control downstream
        }
        finally
        {
            stopwatch.Stop();

            // Exit structured log (Guaranteed to run even if errors occur downstream)
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
```

---

# 🏗️ Session 2: Services Done Right

This session focuses on core Dependency Injection (DI) lifetimes, configuration safety, and search-optimized logging.

## 🛠️ Exercise 2: The Memory Leak (Captive Dependencies)

### 🔴 The Problem Faced
The system background worker `EnrollmentWorker` (which recalculates student scholarships hourly) triggered severe application memory leaks and database connection pool exhaustion under load, eventually resulting in an `OutOfMemoryException`.

* **The Cause:** `EnrollmentWorker` was registered as a **Singleton** (lives forever), but it was directly consuming `IEnrollmentService` in its constructor, which was registered as **Scoped** (intended to live and die with a single HTTP request). 
* When a Singleton captures a Scoped service, the Scoped service is kept alive forever in memory. This anti-pattern is called a **Captive Dependency**. Under load, this locks database connections open and prevents GC from cleaning up request-specific objects.

### 🟢 How We Fixed It
1. **Enabled Strict DI Container Validations**:
   Instructed the host provider to perform deep scope validations during build-time rather than runtime. This causes the application to crash immediately upon startup if any captive dependencies exist.
2. **Introduced `IServiceScopeFactory` Pattern**:
   Refactored `EnrollmentWorker` to inject `IServiceScopeFactory` (a Singleton-safe factory) instead of the scoped service itself.
3. **Manually Managed Short-Lived Scopes**:
   Inside `ProcessBatch()`, we create a short-lived scope wrapped in a `using` statement. This ensures that `IEnrollmentService` is resolved safely inside that specific scope, and once the batch finishes, the scope is disposed and the scoped service (along with any DB connections it holds) is clean-released immediately.

### 💻 Code Highlight: Factory-Scoped Worker
In `TmsApi/EnrollmentWorker.cs`:
```csharp
public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ProcessBatch()
    {
        // Explicitly create a scope to avoid captive dependencies
        using var scope = _scopeFactory.CreateScope();

        // Resolve the Scoped service out of the scope provider
        var enrollmentService = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        // Safe async execution
        var allEnrollments = enrollmentService.GetAllAsync().GetAwaiter().GetResult();
    }
}
```

In `TmsApi/Program.cs`:
```csharp
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// CRITICAL: Force validation checks during building
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
```

---

## 🛠️ Exercise 3: The Silent Crash (Options Pattern)

### 🔴 The Problem Faced
The application depends on an external payment gateway for tuition processing. A previous developer accessed settings using inline strings like `IConfiguration["GatewayUrl"]`. 

* **Why is this dangerous?** If a critical config key is missing from `appsettings.json`, the application starts up smoothly and runs without errors. It is a silent bomb: the moment a student attempts to pay tuition, the app hits the missing setting and crashes with an unexpected `NullReferenceException` in production, ruining the user experience.

### 🟢 How We Fixed It
1. **Established a Strongly-Typed Options Class (`PaymentOptions`)**:
   Mapped the configuration block to a concrete C# class.
2. **Applied Data Annotations for Rules Enforcement**:
   Used `[Required]` to make `GatewayUrl` non-nullable, and `[Range]` to restrict `MaxDepositBirr` to a safe domain ($100$ to $100,000$ Birr).
3. **Instructed Startup Validation (`ValidateOnStart`)**:
   Wired the DI container to bind the options, validate annotations, and fail **instantly** on startup with an explicit `OptionsValidationException` if configuration settings are invalid or missing.

### 💻 Code Highlight: Options with Validation
In `TmsApi/PaymentOptions.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

public class PaymentOptions
{
    [Required(ErrorMessage = "The GatewayUrl field is required.")]
    public required string GatewayUrl { get; init; }

    [Range(100.0, 100000.0, ErrorMessage = "MaxDepositBirr must be between 100 and 100,000.")]
    public decimal MaxDepositBirr { get; init; }
}
```

In `TmsApi/Program.cs`:
```csharp
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart(); // Fail-Fast Startup Validation
```

---

## 🛠️ Exercise 4: The Unsearchable Logs (Structured Logging)

### 🔴 The Problem Faced
When students reported that their enrollment data was failing to save, the IT support team was completely blind because logs were written using **string concatenation/interpolation**:
```csharp
_logger.LogInformation($"Enrolled student {studentId} in course {courseCode}");
```
* **Why did it fail?** String interpolation produces a single, flat, unsearchable string in systems like Seq, Application Insights, or Elasticsearch. It forces support engineers to write slow, expensive full-text regex queries and prevents searching or indexing logs by `StudentId` or `CourseCode` as queryable properties.

### 🟢 How We Fixed It
1. **Transitioned to Message Templates (Structured Logging)**:
   Replaced all concatenated string logs with named placeholders:
   `"Enrolled {StudentId} in {CourseCode} record {EnrollmentId}"`. This causes ASP.NET Core to send the structured parameters to log sinks as independent database-queryable fields.
2. **Established Semantic Log Levels Guidelines**:
   * **`LogInformation`**: For successful core business events (e.g., successful enrollment created or deleted).
   * **`LogWarning`**: For unexpected but recoverable situations that do not crash the app (e.g., duplicate enrollment attempt, requested record not found, delete operation target missing).

### 💻 Code Highlight: Standardized Log Formatting
In `TmsApi/EnrollmentService.cs`:
```csharp
public Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode)
{
    // Check for duplicate enrollment
    var existing = _store.Values
        .FirstOrDefault(e => e.StudentId == studentId && e.CourseCode == courseCode);

    if (existing is not null)
    {
        // LogWarning: Unexpected but recoverable duplicate attempt. Notice structured parameters.
        _logger.LogWarning(
            "Duplicate enrollment attempt {StudentId} already in {CourseCode} (record {EnrollmentId})",
            studentId, courseCode, existing.Id);
        return Task.FromResult(existing);
    }

    var id = Guid.NewGuid().ToString("N")[..8];
    var record = new EnrollmentRecord(id, studentId, courseCode, DateTime.UtcNow);
    _store[id] = record;

    // LogInformation: Successful business event with structured properties
    _logger.LogInformation(
        "Enrolled {StudentId} in {CourseCode} record {EnrollmentId}",
        studentId, courseCode, id);

    return Task.FromResult(record);
}
```

---

# 🌐 Session 3: API Surface & Production Readiness

This session exposes CRUD endpoints, implements RFC-compliant error handling, and builds environment-aware security toggles.

## 🛠️ Exercise 5: The Enrollment API (Controllers with RESTful CRUD)

### 🔴 The Problem Faced
The underlying services were functional, but there was no public-facing API surface. To support a modern Angular front-end, we needed a robust controller interface adhering to strict HTTP REST semantics.

* Common mistakes avoided: returning standard `200 OK` for creation (violates REST standard to provide resource location), or returning blank/arbitrary responses for deletion.

### 🟢 How We Fixed It
1. **Designed a Structured REST Controller**:
   Inherited from `ControllerBase` and utilized `[ApiController]` and route mapping attributes.
2. **Adhered to Strict REST Status Code Specifications**:
   * **`GET /api/enrollments`**: Returns `200 OK` with JSON array.
   * **`GET /api/enrollments/{id}`**: Returns `200 OK` if found, or `404 NotFound` if missing.
   * **`POST /api/enrollments`**: Returns `201 Created` via `CreatedAtAction()`. This automatically appends a `Location` header in the HTTP response indicating the exact URL to fetch the newly created resource (`/api/enrollments/{id}`).
   * **`DELETE /api/enrollments/{id}`**: Returns `204 NoContent` on successful deletion (since no body content is returned), or `404 NotFound` if the target is missing.

### 💻 Code Highlight: REST Controller Design
In `TmsApi/Controllers/EnrollmentsController.cs`:
```csharp
[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _enrollmentService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var record = await _enrollmentService.GetByIdAsync(id);
        return record is not null ? Ok(record) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request)
    {
        var record = await _enrollmentService.EnrollAsync(request.StudentId, request.CourseCode);
        
        // Return 201 Created with Location Header pointing to GetById
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _enrollmentService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
```

---

## 🛠️ Exercise 6: Standardized Problem Details Error Handling (RFC 9457)

### 🔴 The Problem Faced
When unhandled exceptions occurred in the application, the web server would dump messy raw HTML error pages containing complete code stack traces. 

* **Why is this dangerous?**
  1. **Security Vulnerability:** Exposing internal code names, namespaces, database queries, and stack traces gives malicious actors structural maps of our system.
  2. **Frontend Fragmentation:** A front-end framework (like Angular) cannot easily parse diverse raw exceptions. It requires a single, structured, uniform JSON error envelope to map elegant error messages to users.

### 🟢 How We Fixed It
1. **Adopted the RFC 9457 Problem Details Standard**:
   A global standard specifying a uniform JSON shape for API errors.
2. **Registered Global Exception Interception**:
   Added `builder.Services.AddProblemDetails()` and placed `app.UseExceptionHandler()` and `app.UseStatusCodePages()` early in our request pipeline.
3. **Tested Handling via Simulated Failures**:
   Created a custom `TmsDatabaseException` class and a test route `/api/error` to verify that all unhandled exceptions are caught and transformed into standard JSON.

### 💻 Code Highlight: Global Problem Details Configuration
In `TmsApi/Program.cs`:
```csharp
// 1. Register RFC 9457 Problem Details service
builder.Services.AddProblemDetails();

var app = builder.Build();

// 2. Register exception handler early to intercept all downstream crashes
app.UseExceptionHandler();

// 3. Convert empty response status codes (like 404s) into standardized JSON Problem Details
app.UseStatusCodePages();
```

When `/api/error` throws `TmsDatabaseException`, the server returns this clean, professional RFC 9457 JSON payload instead of an HTML stack trace:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "Simulated database failure for ProblemDetails testing"
}
```

---

## 🛠️ Exercise 7: Environment Toggle (Dev vs Prod Readiness)

### 🔴 The Problem Faced
Exposing OpenAPI metadata documents and interactive API explorers (like Scalar) in a public production environment is a major security vulnerability that exposes all API routes and data shapes to malicious actors. However, developers absolutely need these tools in the development environment for active testing.

Additionally, stack traces must be visible during local development but strictly hidden from external users in production.

### 🟢 How We Fixed It
1. **Configured Conditional Route Mapping**:
   Utilized `app.Environment.IsDevelopment()` to restrict Scalar Explorer and OpenAPI endpoint mapping.
2. **Environment-Aware Error Sanitization**:
   Combined `AddProblemDetails()` with `UseExceptionHandler()`. In the **Development** environment, the global handler automatically appends a `"exception"` block containing the stack trace to the JSON response. In the **Production** environment, the framework automatically sanitizes the response, stripping out all developer details and stack traces while maintaining the exact same clean RFC 9457 JSON format.

### 💻 Code Highlight: Environment Guards
In `TmsApi/Program.cs`:
```csharp
// Expose interactive Scalar API Reference ONLY if running in Development environment
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
```

---

## 📊 Summary of Code Verification Checklist (Lab Validation)

We have successfully passed all 11 core verification milestones:

| Session | Tier | Verification Objective | Success Criteria | Status |
| :---: | :---: | :--- | :--- | :---: |
| **S1** | **T1** | Unauthorized Endpoint Protection | GET `/api/assessments/results` without `X-Training-User` returns `401 Unauthorized`. | ✅ Passed |
| **S1** | **T1** | Correlation Tracing | Every response carries `X-Correlation-Id` response header. | ✅ Passed |
| **S1** | **T1** | Request Observability | Entry & Exit structured logs emitted with elapsed times. | ✅ Passed |
| **S2** | **T2** | DI Lifetime Validation | Activating scope validation causes startup failure if a Singleton directly consumes a Scoped service. | ✅ Passed |
| **S2** | **T2** | Captive Dependency Fix | Injected `IServiceScopeFactory` resolving inside worker; parallel worker runs are stable and safe. | ✅ Passed |
| **S2** | **T3** | Configuration Safeguard | Missing configuration properties cause an immediate startup crash (`ValidateOnStart()`), not a runtime error. | ✅ Passed |
| **S2** | **T4** | Searchable Telemetry | Logger uses message templates `{StudentId}` instead of string concatenation with proper Information/Warning levels. | ✅ Passed |
| **S3** | **T5** | HTTP POST REST Semantics | Creating an enrollment returns `201 Created` with a `Location` header to fetch it. | ✅ Passed |
| **S3** | **T5** | HTTP DELETE REST Semantics | Deleting returns `204 NoContent`; subsequent deletion returns `404 NotFound`. | ✅ Passed |
| **S3** | **T5** | Uniform Error Envelopes | System unhandled errors return clean RFC 9457 JSON rather than stack trace dumps. | ✅ Passed |
| **S3** | **T5** | Security Posture Toggling | `/scalar/v1` loaded and interactive in `Development`, but returning `404 NotFound` in `Production`. | ✅ Passed |

---

## 💡 Architectural Best Practices for Presentation
When presenting this to your professor, emphasize these core design patterns:

1. **Fail-Fast (Design Philosophy):**
   We design APIs to fail immediately during startup if misconfigured (Exercise 2 with Scope Validation and Exercise 3 with Options Validation). It is far better to crash the server immediately upon deployment than to let it run and crash later when users attempt to perform a critical action.
2. **Observability (Zero Guesswork):**
   With custom request logging middleware paired with correlation IDs and structured templates (Exercises 1B & 4), production issues are easily traceable. We can isolate a single user session across multiple service steps in seconds.
3. **Clean Interface Separation (REST Compliance):**
   Our endpoints are not just "RPC over HTTP." They respect the REST architectural style by using proper verbs, returning meaningful status codes, and supplying standard navigation pointers like `Location` headers.
