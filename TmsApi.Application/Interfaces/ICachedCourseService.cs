using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

/// <summary>
/// Cached course service contract with stampede protection.
/// Provides hit/miss observability for production monitoring.
/// </summary>
public interface ICachedCourseService
{
    /// <summary>
    /// Get a single course by code with cache stampede protection.
    /// Logs cache HIT or MISS for observability.
    /// </summary>
    Task<CourseResponseDto> GetCourseAsync(string code, CancellationToken ct);

    /// <summary>
    /// Get all courses with cache stampede protection.
    /// Logs cache HIT or MISS for observability.
    /// </summary>
    Task<List<CourseResponseDto>> GetAllCoursesAsync(CancellationToken ct);

    /// <summary>
    /// Get a single course by ID with cache stampede protection.
    /// </summary>
    Task<CourseResponseDto?> GetCourseByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Invalidate all course-related cache entries via tag.
    /// Call this after any write operation (create, update, delete).
    /// </summary>
    Task InvalidateCourseCacheAsync(CancellationToken ct);
}