namespace TmsApi.Application.DTOs;

/// <summary>
/// Response DTO for Enrollment — the wire format clients see.
/// No navigation properties, no EF entities leaked to the API surface.
/// </summary>
public record EnrollmentResponseDto(
    int Id,
    int CourseId,
    int StudentId,
    DateTime EnrolledAt
);
