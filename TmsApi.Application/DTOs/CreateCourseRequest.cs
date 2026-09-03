using System.ComponentModel.DataAnnotations;

namespace TmsApi.Application.DTOs;

/// <summary>
/// Request DTO for creating a new course.
/// Uses DataAnnotations for automatic model validation via [ApiController].
/// Code must match pattern "XXX-000" (e.g., "CSE-101").
/// MaxCapacity is capped between 1 and 200.
/// </summary>
public record CreateCourseRequest
{
    [Required(ErrorMessage = "Course code is required.")]
    [RegularExpression(@"^[A-Z]{3}-\d{3}$",
        ErrorMessage = "Code must follow the pattern XXX-000 (e.g., CSE-101).")]
    public required string Code { get; init; }

    [Required(ErrorMessage = "Course title is required.")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public required string Title { get; init; }

    [Range(1, 200, ErrorMessage = "MaxCapacity must be between 1 and 200.")]
    public int MaxCapacity { get; init; } = 30;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = "General Skills";
    public string Duration { get; init; } = "8 weeks";
}
