using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

/// <summary>
/// Service contract for Course operations.
/// All methods accept CancellationToken for proper request cancellation.
/// Returns DTOs — never EF entities — to maintain the DTO firewall.
/// </summary>
public interface ICourseService
{
    /// <summary>
    /// Get a single course by ID with its enrollment count.
    /// Returns null if not found.
    /// </summary>
    Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Create a new course from validated request data.
    /// Returns the created course DTO with its generated ID.
    /// </summary>
    Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);

    /// <summary>
    /// Check if a course code already exists (for duplicate detection).
    /// Translates to SELECT EXISTS (SELECT 1 ... LIMIT 1).
    /// </summary>
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    /// <summary>
    /// Get a paginated, filterable, sortable list of courses.
    /// Order of operations: Filter → Count → Sort → Skip/Take → Project.
    /// </summary>
    Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct);
}
