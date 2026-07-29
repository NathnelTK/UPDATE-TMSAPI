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

// --- M7 Session 4 - Exercise 7: Whitelist of fields clients may request via ?fields= ---
// nameof(...) makes the whitelist refactor-safe: rename a property and the compiler
// forces you to update the set; remove a property and a build error stops you forgetting.
// This is the security boundary for Data Shaping — only these fields can ever be returned.
public static class CourseDtoFields
{
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(CourseResponseDto.Id),
        nameof(CourseResponseDto.Code),
        nameof(CourseResponseDto.Title),
        nameof(CourseResponseDto.MaxCapacity),
        nameof(CourseResponseDto.EnrollmentCount)
    };
}
