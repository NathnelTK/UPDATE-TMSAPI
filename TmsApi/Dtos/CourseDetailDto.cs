namespace TmsApi.Dtos;

/// <summary>
/// Detail DTO for a single course — includes everything from CourseResponseDto
/// plus a Links array for HATEOAS navigation.
/// Used only in the detail response (GET /api/courses/{id}),
/// not in the list/page response (GET /api/courses).
/// </summary>
public record CourseDetailDto
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Title { get; init; }
    public required int MaxCapacity { get; init; }
    public required int EnrollmentCount { get; init; }
    public required IReadOnlyList<LinkDto> Links { get; init; }
}