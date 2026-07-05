using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

/// <summary>
/// Service implementation for Course operations.
/// All read paths use AsNoTracking() to avoid change-tracking overhead.
/// The create path inserts the entity, saves, then re-reads through GetByIdAsync
/// to return the same projection as the read path.
/// </summary>
public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService
{
    /// <summary>
    /// Get a single course by ID with its enrollment count projected at the query layer.
    /// EF translates c.Enrollments.Count into a SQL COUNT(*) subquery.
    /// </summary>
    public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Create a new course from validated request data.
    /// Maps the DTO to an entity, saves, logs, and re-reads through GetByIdAsync
    /// to return a consistent projection.
    /// </summary>
    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            MaxCapacity = request.MaxCapacity
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);

        // Re-read through the same projection to ensure consistency
        return (await GetByIdAsync(course.Id, ct))!;
    }

    /// <summary>
    /// Check if a course code already exists.
    /// Translates to: SELECT EXISTS (SELECT 1 FROM "Courses" WHERE "Code" = @code LIMIT 1)
    /// </summary>
    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Code == code, ct);

    /// <summary>
    /// Get a paginated, filterable, sortable list of courses.
    /// CRITICAL ORDER: Filter → Count → Sort → Skip/Take → Project.
    /// Counting after Skip/Take would return page count instead of total count.
    /// </summary>
    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
    {
        // Step 1: Start with IQueryable (deferred execution)
        IQueryable<Course> query = context.Courses.AsNoTracking();

        // Step 2: Apply search filter (case-insensitive via ILike for PostgreSQL)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Title, $"%{request.Search}%") ||
                EF.Functions.ILike(c.Code, $"%{request.Search}%"));
        }

        // Step 3: Count BEFORE paging — this produces SELECT COUNT(*)
        var totalCount = await query.CountAsync(ct);

        // Step 4: Apply sorting with whitelist validation
        // Only allow known column names — reject arbitrary strings to prevent SQL injection via LINQ
        query = (request.OrderBy?.ToLowerInvariant()) switch
        {
            "code" => request.Descending
                ? query.OrderByDescending(c => c.Code)
                : query.OrderBy(c => c.Code),
            "maxcapacity" => request.Descending
                ? query.OrderByDescending(c => c.MaxCapacity)
                : query.OrderBy(c => c.MaxCapacity),
            _ => request.Descending
                ? query.OrderByDescending(c => c.Title)
                : query.OrderBy(c => c.Title)
        };

        // Step 5: Apply Skip/Take for pagination, then project to DTO
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .ToListAsync(ct);

        // Step 6: Return paginated response
        return new PagedResponse<CourseResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}