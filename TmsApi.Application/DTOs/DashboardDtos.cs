namespace TmsApi.Application.DTOs;

public record AdminDashboardDto(int TotalStudents, int ActiveStudents, int TotalCourses, int PendingEnrollments, int ActiveEnrollments, int CompletedEnrollments, int PendingGrantApplications, decimal AllocatedGrantAmount, decimal RemainingGrantBudget);
