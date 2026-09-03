namespace TmsApi.Application.DTOs;

/// <summary>
/// Wire format for the student picker (GET /api/v2/students). Read-only projection of
/// the Student entity — no GPA, soft-delete flags, or navigation properties leak out.
/// </summary>
public record StudentListItemDto(
    int Id,
    string Name,
    string RegistrationNumber,
    bool IsActive
);

public record CurrentStudentDto(int Id, string Name, string RegistrationNumber, decimal GPA, bool IsActive);
