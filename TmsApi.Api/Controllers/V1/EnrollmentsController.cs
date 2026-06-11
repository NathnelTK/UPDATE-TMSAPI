using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Api.Controllers.V1;

/// <summary>
/// V1 Enrollments controller GÇö frozen contract.
/// Keeps the nested POST /api/v1/courses/{courseId}/enrollments pattern.
/// Business rules: 404 before 409 GÇö if course doesn't exist, return 404.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/courses/{courseId:int}/enrollments")]
[ApiVersion("1.0")]
public class EnrollmentsController(TmsDbContext context, ILogger<EnrollmentsController> logger) : ControllerBase
{
    /// <summary>
    /// POST /api/v1/courses/{courseId}/enrollments GÇö enroll a student.
    /// Business rule checks in order: course exists (404), capacity (409), create (201).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> EnrollStudent(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct)
    {
        // Step 1: Check course exists
        var course = await context.Courses
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course is null)
            return NotFound();

        // Step 2: Check capacity
        if (course.Enrollments.Count >= course.MaxCapacity)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course is full",
                Detail = $"Course '{course.Title}' has reached its maximum capacity of {course.MaxCapacity}.",
                Status = StatusCodes.Status409Conflict
            });
        }

        // Step 3: Create enrollment
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "V1: Enrolled student {StudentId} in course {CourseId} (enrollment {EnrollmentId})",
            request.StudentId, courseId, enrollment.Id);

        var dto = new EnrollmentResponseDto(enrollment.Id, enrollment.CourseId, enrollment.StudentId, enrollment.EnrolledAt);

        return CreatedAtAction(nameof(EnrollStudent), new { courseId, id = enrollment.Id }, dto);
    }
}
