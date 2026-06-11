using MediatR;

namespace TmsApi.Enrollments.Queries;

/// <summary>
/// Query to get a student's schedule GÇö all courses they are enrolled in.
/// Queries return raw DTOs (no Result wrapper) since reads have no business-rule failure modes.
/// </summary>
public record GetStudentScheduleQuery(int StudentId) : IRequest<ScheduleDto>;

/// <summary>
/// Student schedule DTO GÇö contains a list of enrolled courses.
/// </summary>
public record ScheduleDto(int StudentId, List<ScheduleItemDto> Courses);

/// <summary>
/// Individual schedule item GÇö course code, title, and meeting info.
/// </summary>
public record ScheduleItemDto(string CourseCode, string Title, string Schedule);
