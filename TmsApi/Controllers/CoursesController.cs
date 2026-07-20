using Microsoft.AspNetCore.Mvc;
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
public class CoursesController(ICourseService courseService) : ControllerBase
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
    /// GET /api/courses/{id} — single course by ID with enrollment count.
    /// Returns 404 if not found.
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
      
// TODO 3: Call courseService.GetByIdAsync(id, ct).
//Return Ok(course) when the result is not null.
//Return NotFound() when the result is null.
        var course = await courseService.GetByIdAsync(id, ct);
        return course is not null ? Ok(course) : NotFound();
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
// TODO 4: Call courseService.CreateAsync(course, ct).
//Return CreatedAtAction(nameof(GetCourseById), new {id = result.Id }, result).
//ly.
        var result = await courseService.CreateAsync(request, ct);

        return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
    }
}