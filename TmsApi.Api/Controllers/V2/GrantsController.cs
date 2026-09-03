using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/grants")]
[ApiVersion("2.0")]
public sealed class GrantsController(TmsDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<GrantProgramResponseDto>>> GetPrograms(CancellationToken ct)
    {
        var rows = await db.GrantPrograms.AsNoTracking().Where(x => x.IsActive)
            .Select(x => new GrantProgramResponseDto(x.Id, x.Name, x.Description, x.FundingOrganization, x.TotalBudget, x.AmountPerStudent, x.Applications.Where(a => a.Status == GrantStatus.Approved).SelectMany(a => a.Allocation == null ? new List<GrantAllocation>() : new List<GrantAllocation> { a.Allocation }).Sum(a => (decimal?)a.Amount) ?? 0, 0, x.IsActive)).ToListAsync(ct);
        return Ok(rows.Select(x => x with { RemainingBudget = x.TotalBudget - x.AllocatedAmount }).ToList());
    }

    [HttpPost("applications")]
    [Authorize]
    public async Task<IActionResult> Apply(ApplyGrantRequest request, CancellationToken ct)
    {
        var studentId = await OwnStudentIdAsync(ct);
        if (studentId is null && !User.IsInRole("Admin")) return Forbid();
        if (studentId is null) return Problem(statusCode: 422, title: "Student account is not linked");
        var program = await db.GrantPrograms.FirstOrDefaultAsync(x => x.Id == request.GrantProgramId && x.IsActive && x.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow) && x.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow), ct);
        if (program is null) return NotFound("Grant program not found or closed.");
        if (!await db.Students.AnyAsync(x => x.Id == studentId && x.IsActive && !x.IsDeleted, ct)) return Problem(statusCode: 422, title: "Student is not eligible", detail: "Only active students may apply.");
        if (await db.GrantApplications.AnyAsync(x => x.StudentId == studentId && x.GrantProgramId == request.GrantProgramId && x.Status != GrantStatus.Cancelled, ct)) return Conflict("A grant application already exists for this student.");
        db.GrantApplications.Add(new GrantApplication { StudentId = studentId.Value, GrantProgramId = request.GrantProgramId, EligibilityScore = 100 });
        await db.SaveChangesAsync(ct);
        return StatusCode(201);
    }

    [HttpGet("applications/my")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<GrantApplicationResponseDto>>> MyApplications(CancellationToken ct)
    {
        var studentId = await OwnStudentIdAsync(ct);
        if (studentId is null) return Forbid();
        return Ok(await ProjectApplications().Where(x => x.StudentId == studentId).OrderByDescending(x => x.ApplicationDate).ToListAsync(ct));
    }

    [HttpGet("applications")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<ActionResult<IReadOnlyList<GrantApplicationResponseDto>>> Applications(CancellationToken ct) => Ok(await ProjectApplications().OrderByDescending(x => x.ApplicationDate).ToListAsync(ct));

    [HttpPost("applications/{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public Task<IActionResult> Approve(int id, ReviewGrantRequest request, CancellationToken ct) => ReviewAsync(id, GrantStatus.Approved, request, ct);

    [HttpPost("applications/{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public Task<IActionResult> Reject(int id, ReviewGrantRequest request, CancellationToken ct) => ReviewAsync(id, GrantStatus.Rejected, request, ct);

    private async Task<IActionResult> ReviewAsync(int id, GrantStatus status, ReviewGrantRequest request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var app = await db.GrantApplications.Include(x => x.GrantProgram).Include(x => x.Allocation).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (app is null) return NotFound();
        if (app.Status != GrantStatus.Submitted && app.Status != GrantStatus.UnderReview) return Conflict("This application cannot be reviewed in its current status.");
        if (status == GrantStatus.Approved)
        {
            var allocated = await db.GrantAllocations.Where(x => x.GrantApplication.GrantProgramId == app.GrantProgramId).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
            if (allocated + app.GrantProgram.AmountPerStudent > app.GrantProgram.TotalBudget) return Conflict("Grant budget exceeded.");
            app.Allocation = new GrantAllocation { StudentId = app.StudentId, Amount = app.GrantProgram.AmountPerStudent };
        }
        app.Status = status; app.ReviewNotes = request.ReviewNotes; app.ReviewedBy = User.Identity?.Name; app.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return NoContent();
    }

    private IQueryable<GrantApplicationResponseDto> ProjectApplications() => db.GrantApplications.AsNoTracking().Select(x => new GrantApplicationResponseDto(x.Id, x.StudentId, x.GrantProgramId, x.GrantProgram.Name, x.Status, x.EligibilityScore, x.ApplicationDate, x.ReviewNotes));
    private async Task<int?> OwnStudentIdAsync(CancellationToken ct) => await db.Students.Where(x => x.UserId == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
}
