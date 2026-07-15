using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers;

/// <summary>
/// REST controller for Course resources.
/// Routes: /api/courses (collection) and /api/courses/{id} (single item).
/// All methods return DTOs — never EF entities.
/// Business rule errors return 409 Conflict with ProblemDetails body.
/// </summary>
[ApiController]
[Route("api/courses")]
public class CoursesController(
    ICourseService courseService,
    LinkGenerator linkGenerator) : ControllerBase
{
    /// <summary>
    /// GET /api/courses — paginated, filterable, sortable list of courses.
    /// Query parameters: page, pageSize, search, orderBy, descending.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        var result = await courseService.GetCoursesAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/courses/{id} — single course with HATEOAS links.
    /// Returns CourseDetailDto including self, update, delete, enrollments links,
    /// and a conditional enroll link (only when course has capacity).
    /// Returns 404 if not found.
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);
        if (course is null) return NotFound();

        // Build HATEOAS links using LinkGenerator — never string interpolation
        var links = new List<LinkDto>
        {
            new(
                Href: linkGenerator.GetPathByName(HttpContext, nameof(GetCourseById), new { id })!,
                Rel: "self",
                Method: "GET"),
            new(
                Href: linkGenerator.GetPathByName(HttpContext, nameof(GetCourseById), new { id })!,
                Rel: "update",
                Method: "PUT"),
            new(
                Href: linkGenerator.GetPathByName(HttpContext, nameof(GetCourseById), new { id })!,
                Rel: "delete",
                Method: "DELETE"),
            new(
                Href: linkGenerator.GetPathByAction(HttpContext, action: nameof(EnrollmentsController.GetEnrollments), controller: "Enrollments", values: new { courseId = id })!,
                Rel: "enrollments",
                Method: "GET")
        };

        // Conditional link — only show Enrol button when the course has capacity
        if (course.EnrollmentCount < course.MaxCapacity)
        {
            links.Add(new(
                Href: linkGenerator.GetPathByAction(HttpContext, action: nameof(EnrollmentsController.GetEnrollments), controller: "Enrollments", values: new { courseId = id })!,
                Rel: "enroll",
                Method: "POST"));
        }

        var detail = new CourseDetailDto
        {
            Id = course.Id,
            Code = course.Code,
            Title = course.Title,
            MaxCapacity = course.MaxCapacity,
            EnrollmentCount = course.EnrollmentCount,
            Links = links.AsReadOnly()
        };

        return Ok(detail);
    }

    /// <summary>
    /// POST /api/courses — create a new course.
    /// Returns 201 Created with Location header pointing to GetCourseById.
    /// Returns 409 Conflict if the course code already exists.
    /// Returns 400 Bad Request with ValidationProblemDetails if input is invalid.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCourse(
        CreateCourseRequest request,
        CancellationToken ct)
    {
        // Business rule: duplicate course code check
        if (await courseService.CodeExistsAsync(request.Code, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course code already exists",
                Detail = $"A course with code '{request.Code}' is already registered.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var result = await courseService.CreateAsync(request, ct);

        return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
    }
}
