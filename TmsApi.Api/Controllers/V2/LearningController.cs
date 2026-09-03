using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}")]
[ApiVersion("2.0")]
public sealed class LearningController(TmsDbContext db) : ControllerBase
{
    [HttpGet("attendance")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<AttendanceResponseDto>>> GetAttendance([FromQuery] int? studentId, [FromQuery] int? courseId, CancellationToken ct)
    {
        var query = db.AttendanceRecords.AsNoTracking().AsQueryable();
        if (studentId.HasValue) query = query.Where(x => x.StudentId == studentId.Value);
        if (courseId.HasValue) query = query.Where(x => x.CourseId == courseId.Value);
        if (!User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var ownId = await OwnStudentIdAsync(ct);
            if (ownId is null) return Forbid();
            query = query.Where(x => x.StudentId == ownId.Value);
        }
        return Ok(await query.OrderByDescending(x => x.Date).Select(x => new AttendanceResponseDto(x.Id, x.StudentId, x.CourseId, x.Date, x.Status, x.Remarks)).ToListAsync(ct));
    }

    [HttpPost("attendance")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> RecordAttendance(RecordAttendanceRequest request, CancellationToken ct)
    {
        var valid = await db.Students.AnyAsync(x => x.Id == request.StudentId && x.IsActive && !x.IsDeleted, ct)
            && await db.Courses.AnyAsync(x => x.Id == request.CourseId, ct);
        if (!valid) return Problem(statusCode: 404, title: "Student or course not found");
        var item = await db.AttendanceRecords.FirstOrDefaultAsync(x => x.StudentId == request.StudentId && x.CourseId == request.CourseId && x.Date == request.Date, ct);
        if (item is null) db.AttendanceRecords.Add(new Attendance { StudentId = request.StudentId, CourseId = request.CourseId, Date = request.Date, Status = request.Status, Remarks = request.Remarks });
        else { item.Status = request.Status; item.Remarks = request.Remarks; }
        await db.SaveChangesAsync(ct);
        return item is null ? StatusCode(201) : NoContent();
    }

    [HttpGet("grades")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<GradeResponseDto>>> GetGrades([FromQuery] int? studentId, CancellationToken ct)
    {
        var query = db.Grades.AsNoTracking().Include(x => x.Assessment).AsQueryable();
        if (studentId.HasValue) query = query.Where(x => x.StudentId == studentId.Value);
        if (!User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var ownId = await OwnStudentIdAsync(ct);
            if (ownId is null) return Forbid();
            query = query.Where(x => x.StudentId == ownId.Value);
        }
        return Ok(await query.OrderBy(x => x.Assessment.Title).Select(x => new GradeResponseDto(x.Id, x.AssessmentId, x.StudentId, x.Assessment.Title, x.Score, x.Assessment.MaxScore, x.Assessment.Weight, x.Remarks, x.GradedAt)).ToListAsync(ct));
    }

    [HttpPost("grades")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> RecordGrade(RecordGradeRequest request, CancellationToken ct)
    {
        var assessment = await db.Assessments.FindAsync([request.AssessmentId], ct);
        if (assessment is null) return Problem(statusCode: 404, title: "Assessment not found");
        if (request.Score < 0 || request.Score > assessment.MaxScore) return Problem(statusCode: 422, title: "Invalid grade", detail: "Score must be between zero and the assessment maximum.");
        var grade = await db.Grades.FirstOrDefaultAsync(x => x.AssessmentId == request.AssessmentId && x.StudentId == request.StudentId, ct);
        if (grade is null) db.Grades.Add(new Grade { AssessmentId = request.AssessmentId, StudentId = request.StudentId, Score = request.Score, Remarks = request.Remarks, GradedBy = User.Identity?.Name });
        else { grade.Score = request.Score; grade.Remarks = request.Remarks; grade.GradedAt = DateTime.UtcNow; grade.GradedBy = User.Identity?.Name; }
        await db.SaveChangesAsync(ct);
        return grade is null ? StatusCode(201) : NoContent();
    }

    [HttpGet("students/{studentId:int}/summary")]
    [Authorize]
    public async Task<ActionResult<StudentLearningSummaryDto>> GetSummary(int studentId, CancellationToken ct)
    {
        if (!await CanAccessStudentAsync(studentId, ct)) return Forbid();
        var attendance = db.AttendanceRecords.Where(x => x.StudentId == studentId);
        var total = await attendance.CountAsync(ct);
        var present = await attendance.CountAsync(x => x.Status == AttendanceStatus.Present || x.Status == AttendanceStatus.Late, ct);
        var grades = db.Grades.Include(x => x.Assessment).Where(x => x.StudentId == studentId);
        var weighted = await grades.Select(x => x.Assessment.Weight == 0 ? 0 : x.Score / x.Assessment.MaxScore * x.Assessment.Weight).SumAsync(ct);
        var unread = await db.Notifications.CountAsync(x => x.UserId == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value && !x.IsRead, ct);
        return Ok(new StudentLearningSummaryDto(studentId, total, present, total - present, total == 0 ? 0 : Math.Round(present * 100m / total, 2), Math.Round(weighted, 2), unread));
    }

    [HttpGet("notifications/my")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<NotificationResponseDto>>> GetNotifications(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();
        return Ok(await db.Notifications.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).Select(x => new NotificationResponseDto(x.Id, x.Title, x.Message, x.Type, x.IsRead, x.CreatedAt)).ToListAsync(ct));
    }

    private async Task<int?> OwnStudentIdAsync(CancellationToken ct) => await db.Students.Where(x => x.UserId == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
    private async Task<bool> CanAccessStudentAsync(int studentId, CancellationToken ct) => User.IsInRole("Admin") || User.IsInRole("Instructor") || await db.Students.AnyAsync(x => x.Id == studentId && x.UserId == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value, ct);
}
