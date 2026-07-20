namespace TmsApi.Application.DTOs;

/// <summary>
/// Response DTO for Course — the wire format clients see.
/// No navigation properties, no EF entities leaked to the API surface.
/// EnrollmentCount is computed at the query layer via SQL subquery.
/// </summary>
public record CourseResponseDto(
    int Id,
    string Code,
    string Title,
    int MaxCapacity,
    int EnrollmentCount
);
