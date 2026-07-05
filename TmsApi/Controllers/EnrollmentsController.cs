using Microsoft.AspNetCore.Mvc;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers;

/// <summary>
/// REST controller for Enrollment resources nested under courses.
/// Routes: /api/courses/{courseId}/enrollments
/// Business rule: 404 before 409 — if course doesn't exist, return 404,
/// not a misleading 409 about capacity.
/// </summary>
[ApiController]
[Route("api/courses/{courseId:int}/enrollments")]
public class EnrollmentsController(
    ICourseService courseService,
    IEnrollmentService enrollmentService) : ControllerBase
{
    /// <summary>
    /// GET /api/courses/{courseId}/enrollments/{id} — single enrollment.
    /// Returns 404 if not found.
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetEnrollment))]
    public async Task<IActionResult> GetEnrollment(int courseId, int id, CancellationToken ct)
    {
        var enrollment = await enrollmentService.GetByIdAsync(courseId, id, ct);
        return enrollment is not null ? Ok(enrollment) : NotFound();
    }

    /// <summary>
    /// POST /api/courses/{courseId}/enrollments — enroll a student in a course.
    /// Business rule checks in order:
    /// 1. Course must exist (404 if not)
    /// 2. Course must have capacity (409 if full)
    /// 3. Create enrollment (201 on success)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> EnrollStudent(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct)
    {
        // Step 1: Check course exists — 404 before 409
        var course = await courseService.GetByIdAsync(courseId, ct);
        if (course is null)
            return NotFound();

        // Step 2: Check capacity — return 409 Conflict if full
        if (course.EnrollmentCount >= course.MaxCapacity)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course is full",
                Detail = $"Course '{course.Title}' has reached its maximum capacity of {course.MaxCapacity}.",
                Status = StatusCodes.Status409Conflict
            });
        }

        // Step 3: Create enrollment
        var enrollment = await enrollmentService.CreateAsync(courseId, request, ct);

        return CreatedAtAction(nameof(GetEnrollment), new { courseId, id = enrollment.Id }, enrollment);
    }
}