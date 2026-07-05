using System.ComponentModel.DataAnnotations;

namespace TmsApi.Dtos;

/// <summary>
/// Request DTO for enrolling a student into a course.
/// StudentId must be a positive integer.
/// </summary>
public record EnrollStudentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "StudentId must be a positive integer.")]
    public required int StudentId { get; init; }
}