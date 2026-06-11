using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// --- Session 3 - Exercise 6: Standardized RFC 9457 Problem Details Service ---
builder.Services.AddProblemDetails();

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

// --- Session 3 - Exercise 6: UseExceptionHandler early in the pipeline ---
app.UseExceptionHandler();

// --- Session 3 - Exercise 6: UseStatusCodePages to convert empty status code responses to Problem Details ---
app.UseStatusCodePages();

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
app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
{
    worker.ProcessBatch();
    return Results.Ok("processed");
});

// --- Session 3 - Exercise 6: Simulated Error Route ---
// Intentionally throws a TmsDatabaseException to test RFC 9457 Problem Details formatting.
app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});

app.Run();
