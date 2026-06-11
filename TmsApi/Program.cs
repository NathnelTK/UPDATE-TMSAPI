using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// --- Session 1 - Exercise 1: Registering Authentication and Authorization Services ---
// We register the custom "Training" authentication scheme that uses our TrainingAuthHandler.
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

var app = builder.Build();

// --- Session 1 - Exercise 1B: Middleware Ordering ---
// 1. RequestLoggingMiddleware is registered first as the outer wrapper
app.UseMiddleware<RequestLoggingMiddleware>();

// 2. UseExceptionHandler is registered next to catch all downstream exceptions
app.UseExceptionHandler();

// 3. UseHttpsRedirection
app.UseHttpsRedirection();

// 4. UseRouting
app.UseRouting();

// 5. UseAuthentication and UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- Session 1 - Exercise 1: Secured Minimal API Endpoint ---
// This endpoint requires authorization, so callers without a valid "X-Training-User" header will receive a 401.
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
})).RequireAuthorization();

app.Run();
