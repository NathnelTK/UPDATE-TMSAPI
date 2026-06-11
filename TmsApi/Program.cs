using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// --- Session 1 - Exercise 1: Registering Authentication and Authorization Services ---
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

// --- Session 2 - Exercise 2: Dependency Injection Registrations ---
// 1. EnrollmentWorker registered as a Singleton.
builder.Services.AddSingleton<EnrollmentWorker>();
// 2. IEnrollmentService registered as a Scoped service.
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// --- Session 2 - Exercise 2: Active DI Container Validation ---
// Force scope and dependency validations during building/startup. This detects captive
// dependencies (e.g. Asingleton trying to directly consume a scoped service) early.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

var app = builder.Build();

// --- Session 1 - Exercise 1B: Middleware Ordering ---
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- Session 1 - Exercise 1: Secured Minimal API Endpoint ---
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
})).RequireAuthorization();

// --- Session 2 - Exercise 2: Enrollment Worker Smoke Test Route ---
// Allows triggering the singleton EnrollmentWorker, which internally resolves
// the scoped EnrollmentService from a manual scope.
app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
{
    worker.ProcessBatch();
    return Results.Ok("processed");
});

app.Run();
