using TmsApi.Dtos;

namespace TmsApi.Services;

/// <summary>
/// Service contract for Enrollment operations nested under courses.
/// Routes follow the pattern: api/courses/{courseId}/enrollments
/// </summary>
public interface IEnrollmentService
{
    /// <summary>
    /// Get a single enrollment by its own ID, scoped to a specific course.
    /// Returns null if not found.
    /// </summary>
    Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct);

    /// <summary>
    /// Create a new enrollment for a student in a course.
    /// Business rule checks (course exists, capacity not full) are expected
    /// to be handled by the controller before calling this.
    /// </summary>
    Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct);
}