using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

/// <summary>
/// V2 Courses controller - returns the wrapped envelope format.
/// Response shape: { data, meta, links }
/// This is the Module 7 spine format that the Angular team will use going forward.
/// Uses cached service for stampede protection.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController(ICachedCourseService cachedService, TmsDbContext context) : ControllerBase
{
    /// <summary>
    /// GET /api/v2/courses - paginated list with data/meta/links envelope.
    /// Uses HybridCache for stampede protection.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        // Use cached service to get all courses
        var allCourses = await cachedService.GetAllCoursesAsync(ct);

        var totalCount = allCourses.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var rows = allCourses
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var hasNext = page < totalPages;
        var hasPrevious = page > 1;

        return Ok(new
        {
            data = rows,
            meta = new
            {
                totalCount,
                page,
                pageSize,
                totalPages,
                hasNext,
                hasPrevious
            },
            links = new
            {
                self = $"/api/v2/courses?page={page}&pageSize={pageSize}",
                next = hasNext
                    ? $"/api/v2/courses?page={page + 1}&pageSize={pageSize}"
                    : (string?)null,
                prev = hasPrevious
                    ? $"/api/v2/courses?page={page - 1}&pageSize={pageSize}"
                    : (string?)null,
                enroll = "/api/v2/enrollments"
            }
        });
    }

    /// <summary>
    /// GET /api/v2/courses/{id} - single course by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await cachedService.GetCourseByIdAsync(id, ct);
        return course is not null ? Ok(course) : NotFound();
    }

    /// <summary>
    /// PUT /api/v2/courses/{id} - update a course (triggers cache invalidation).
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(
        int id,
        [FromBody] UpdateCourseRequest request,
        CancellationToken ct)
    {
        var course = await context.Courses.FindAsync([id], ct);
        if (course is null)
            return NotFound();

        course.Title = request.Title;
        await context.SaveChangesAsync(ct);

        // Invalidate cache after write
        await cachedService.InvalidateCourseCacheAsync(ct);

        return NoContent();
    }
}

/// <summary>
/// Request DTO for updating a course.
/// </summary>
public record UpdateCourseRequest(string Title);
