using TmsApi.Domain.Entities;

namespace TmsApi.Application.DTOs;

public record AttendanceResponseDto(int Id, int StudentId, int CourseId, DateOnly Date, AttendanceStatus Status, string? Remarks);
public record RecordAttendanceRequest(int StudentId, int CourseId, DateOnly Date, AttendanceStatus Status, string? Remarks);
public record GradeResponseDto(int Id, int AssessmentId, int StudentId, string AssessmentTitle, decimal Score, decimal MaximumScore, decimal Weight, string? Remarks, DateTime GradedAt);
public record RecordGradeRequest(int AssessmentId, int StudentId, decimal Score, string? Remarks);
public record StudentLearningSummaryDto(int StudentId, int TotalSessions, int PresentSessions, int AbsentSessions, decimal AttendancePercentage, decimal WeightedScore, int UnreadNotifications);
public record NotificationResponseDto(int Id, string Title, string Message, string Type, bool IsRead, DateTime CreatedAt);
public record GrantProgramResponseDto(int Id, string Name, string Description, string FundingOrganization, decimal TotalBudget, decimal AmountPerStudent, decimal AllocatedAmount, decimal RemainingBudget, bool IsActive);
public record GrantApplicationResponseDto(int Id, int StudentId, int GrantProgramId, string GrantProgramName, GrantStatus Status, decimal EligibilityScore, DateTime ApplicationDate, string? ReviewNotes);
public record ApplyGrantRequest(int GrantProgramId);
public record ReviewGrantRequest(string? ReviewNotes);
