namespace TmsApi.Application.DTOs;

/// <summary>
/// Wire format for a row in the enrollment approval queue (GET /api/v2/enrollments).
/// Flattens the Student/Course navigation properties into display names so the Angular
/// grid can bind directly, and exposes <c>Status</c> as a readable string
/// ("Pending" / "Approved" / "Rejected").
/// </summary>
public record EnrollmentListItemDto(
    int Id,
    int StudentId,
    string StudentName,
    int CourseId,
    string CourseCode,
    string CourseName,
    string Status,
    DateTime EnrolledAt
);
