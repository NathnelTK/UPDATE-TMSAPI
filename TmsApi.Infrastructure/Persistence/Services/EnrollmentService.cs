using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Interfaces;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Services;

/// <summary>
/// Service implementation for Enrollment operations nested under courses.
/// All read paths use AsNoTracking() to avoid change-tracking overhead.
/// The create path inserts, saves, logs, then re-reads through GetByIdAsync.
/// </summary>
public class EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    /// <summary>
    /// Get a single enrollment by its own ID, scoped to a specific course.
    /// Returns null if not found.
    /// </summary>
    public Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt))
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Create a new enrollment for a student in a course.
    /// Business rule checks (course exists, capacity not full) are expected
    /// to be handled by the controller before calling this.
    /// </summary>
    public async Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct)
    {
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Enrolled student {StudentId} in course {CourseId} (enrollment {EnrollmentId})",
            request.StudentId, courseId, enrollment.Id);

        // Re-read through the same projection to ensure consistency
        return (await GetByIdAsync(courseId, enrollment.Id, ct))!;
    }
}
