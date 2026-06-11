using Microsoft.AspNetCore.Authentication;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

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

var app = builder.Build();

// --- Session 1 - Exercise 1B: Middleware Ordering ---
app.UseMiddleware<RequestLoggingMiddleware>();

// --- Session 3 - Exercise 6 & 7: Environment-Aware Error Handling ---
// We place UseExceptionHandler early in the pipeline to catch all downstream errors.
// By combining AddProblemDetails() with UseExceptionHandler(), stack traces are hidden from
// external users automatically while keeping RFC 9457 JSON Problem Details returned in both environments.
app.UseExceptionHandler();

// --- Session 3 - Exercise 6: UseStatusCodePages to convert empty status code responses to Problem Details ---
app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- Session 3 - Exercise 7: Environment-Aware OpenAPI & Scalar Explorer ---
// Expose OpenAPI and Scalar interactive reference ONLY in Development environment.
// In Production, accessing /scalar/v1 will return 404.
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

app.Run();
