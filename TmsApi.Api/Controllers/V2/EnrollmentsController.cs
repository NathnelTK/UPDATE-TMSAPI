using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Enrollments.Commands;
using TmsApi.Enrollments.Queries;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

/// <summary>
/// V2 Enrollments controller — CQRS reads/writes via MediatR, with a lightweight
/// approval workflow layered on top.
/// GET    /api/v2/enrollments                     — approval queue (anonymous read).
/// POST   /api/v2/enrollments                     — enroll a student by course code.
/// GET    /api/v2/enrollments/{studentId}/schedule — a student's enrolled courses.
/// POST   /api/v2/enrollments/{id}/approve|reject  — registrar decision (auth required).
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/enrollments")]
[ApiVersion("2.0")]
public class EnrollmentsController(IMediator mediator, TmsDbContext db) : ControllerBase
{
    /// <summary>
    /// GET /api/v2/enrollments — the full approval queue with student/course names.
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Instructor")) return Forbid();
        var rows = await mediator.Send(new GetAllEnrollmentsQuery(), ct);
        return Ok(rows);
    }

    /// <summary>
    /// POST /api/v2/enrollments — enroll a student by course code.
    /// Maps Result&lt;T,E&gt; to correct HTTP status codes via Match().
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Enroll(
        EnrollStudentCommand command,
        CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var ownsStudent = userId is not null && await db.Students.AnyAsync(s => s.Id == command.StudentId && s.UserId == userId, ct);
            if (!ownsStudent) return Forbid();
        }
        var result = await mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: created => CreatedAtAction(
                nameof(GetSchedule),
                new { studentId = created.StudentId },
                created),
            onFailure: error =>
            {
                var status = error.Code switch
                {
                    "course_not_found" => StatusCodes.Status404NotFound,
                    "course_full" or "already_enrolled" => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status400BadRequest
                };

                return Problem(
                    statusCode: status,
                    title: "Enrollment rejected",
                    detail: error.Message,
                    type: $"https://tms.local/errors/{error.Code}");
            });
    }

    /// <summary>
    /// GET /api/v2/enrollments/{studentId}/schedule — get a student's enrolled courses.
    /// </summary>
    [Authorize]
    [HttpGet("{studentId:int}/schedule")]
    public async Task<IActionResult> GetSchedule(
        int studentId, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId is null || !await db.Students.AnyAsync(s => s.Id == studentId && s.UserId == userId, ct)) return Forbid();
        }
        var schedule = await mediator.Send(
            new GetStudentScheduleQuery(studentId), ct);
        return Ok(schedule);
    }

    /// <summary>
    /// POST /api/v2/enrollments/{id}/approve — registrar approves a pending enrollment.
    /// Requires a valid JWT so the bearer token is genuinely exercised end-to-end.
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin,Instructor")]
    public Task<IActionResult> Approve(int id, CancellationToken ct)
        => SetStatusAsync(id, EnrollmentStatus.Approved, ct);

    /// <summary>
    /// POST /api/v2/enrollments/{id}/reject — registrar rejects a pending enrollment.
    /// </summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin,Instructor")]
    public Task<IActionResult> Reject(int id, CancellationToken ct)
        => SetStatusAsync(id, EnrollmentStatus.Rejected, ct);

    private async Task<IActionResult> SetStatusAsync(
        int id, EnrollmentStatus status, CancellationToken ct)
    {
        var enrollment = await db.Enrollments.FindAsync([id], ct);
        if (enrollment is null)
            return NotFound();

        enrollment.Status = status;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
