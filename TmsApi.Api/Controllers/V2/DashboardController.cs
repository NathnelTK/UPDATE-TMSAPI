using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/admin/dashboard")]
[ApiVersion("2.0")]
[Authorize(Roles = "Admin")]
public sealed class DashboardController(TmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminDashboardDto>> Get(CancellationToken ct)
    {
        var totalStudents = await db.Students.IgnoreQueryFilters().CountAsync(x => !x.IsDeleted, ct);
        var activeStudents = await db.Students.CountAsync(x => x.IsActive, ct);
        var pending = await db.Enrollments.CountAsync(x => x.Status == Domain.Entities.EnrollmentStatus.Pending && !x.IsArchived, ct);
        var active = await db.Enrollments.CountAsync(x => x.Status == Domain.Entities.EnrollmentStatus.Approved && !x.IsArchived, ct);
        var completed = await db.Enrollments.CountAsync(x => x.Grade != null && !x.IsArchived, ct);
        var pendingGrants = await db.GrantApplications.CountAsync(x => x.Status == Domain.Entities.GrantStatus.Submitted || x.Status == Domain.Entities.GrantStatus.UnderReview, ct);
        var allocated = await db.GrantAllocations.SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var budget = await db.GrantPrograms.Where(x => x.IsActive).SumAsync(x => (decimal?)x.TotalBudget, ct) ?? 0;
        return Ok(new AdminDashboardDto(totalStudents, activeStudents, await db.Courses.CountAsync(ct), pending, active, completed, pendingGrants, allocated, Math.Max(0, budget - allocated)));
    }
}
