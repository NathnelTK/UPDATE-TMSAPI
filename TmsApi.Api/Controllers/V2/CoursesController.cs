using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Common;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Utilities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

/// <summary>
/// V2 Courses controller - returns the wrapped envelope format.
/// Response shape: { data, meta, links }
/// This is the Module 7 spine format that the Angular team will use going forward.
/// Uses cached service for stampede protection.
/// M7 Session 4 - Exercise 7: adds Data Shaping (?fields=) with whitelist security
/// and a small honest HATEOAS link set (self/next/prev + one action link).
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController(ICachedCourseService cachedService, TmsDbContext context) : ControllerBase
{
    /// <summary>
    /// GET /api/v2/courses - paginated list with data/meta/links envelope.
    /// Uses HybridCache for stampede protection.
    /// M7 Session 4 - Exercise 7: supports ?fields= selection with whitelist validation.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] string? fields,
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

        // --- M7 Session 4 - Exercise 7: Data Shaping with whitelist security ---
        // ShapeData throws BadRequestException for unknown/dangerous field names,
        // which is mapped to HTTP 400 by the exception handler.
        var shaped = rows.ShapeData(fields, CourseDtoFields.Allowed);

        // --- M7 Session 4 - Exercise 7: HATEOAS links (small honest set) ---
        // self, next, prev for collections — decouples client from URL structure.
        var links = new List<LinkDto>
        {
            new(Url.Action(nameof(GetCourses), new { page, pageSize, fields })!, "self", "GET")
        };
        if (hasNext)
            links.Add(new(Url.Action(nameof(GetCourses), new { page = page + 1, pageSize, fields })!, "next", "GET"));
        if (hasPrevious)
            links.Add(new(Url.Action(nameof(GetCourses), new { page = page - 1, pageSize, fields })!, "prev", "GET"));

        return Ok(new
        {
            data = shaped,
            meta = new
            {
                totalCount,
                page,
                pageSize,
                totalPages,
                hasNext,
                hasPrevious
            },
            links
        });
    }

    /// <summary>
    /// GET /api/v2/courses/{id} - single course by ID.
    /// M7 Session 4 - Exercise 7: includes self link plus exactly one action link (enroll).
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await cachedService.GetCourseByIdAsync(id, ct);
        if (course is null) return NotFound();

        // --- M7 Session 4 - Exercise 7: HATEOAS — self plus exactly one action link ---
        return Ok(new
        {
            data = course,
            links = new[]
            {
                new LinkDto(Url.Action(nameof(GetCourseById), new { id })!, "self", "GET"),
                new LinkDto(Url.Action("Enroll", "Enrollments", new { })!, "enroll", "POST")
            }
        });
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

    /// <summary>
    /// DELETE /api/v2/courses/{id} - remove a course.
    /// M10 Session 3 - Exercise 3: returns 409 Conflict as an RFC 7807
    /// ProblemDetails payload when the course still has active enrollments, so the
    /// Angular SignalStore's optimistic delete rolls back and restores the row.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken ct)
    {
        var course = await context.Courses.FindAsync([id], ct);
        if (course is null)
            return NotFound();

        var hasEnrollments = await context.Enrollments.AnyAsync(e => e.CourseId == id, ct);
        if (hasEnrollments)
        {
            return Problem(
                title: "Course has active enrollments",
                detail: $"Cannot delete course {course.Code}: active student enrollments exist.",
                statusCode: StatusCodes.Status409Conflict);
        }

        context.Courses.Remove(course);
        await context.SaveChangesAsync(ct);
        await cachedService.InvalidateCourseCacheAsync(ct);

        return NoContent();
    }
}

/// <summary>
/// Request DTO for updating a course.
/// </summary>
public record UpdateCourseRequest(string Title);