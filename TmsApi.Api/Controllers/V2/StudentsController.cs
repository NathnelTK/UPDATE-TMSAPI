using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

/// <summary>
/// V2 Students controller — read-only list used by the Angular enrollment form's
/// student picker and to resolve student names in the UI. Anonymous, like the course
/// catalog, so the picker loads on the enrollment screen without extra round-trips.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/students")]
[ApiVersion("2.0")]
public class StudentsController(TmsDbContext context) : ControllerBase
{
    /// <summary>
    /// GET /api/v2/students — active (non-deleted) students, ordered by name.
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetStudents(CancellationToken ct)
    {
        var students = await context.Students
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Name)
            .Select(s => new StudentListItemDto(
                s.Id, s.Name, s.RegistrationNumber, s.IsActive))
            .ToListAsync(ct);

        return Ok(students);
    }

    [Authorize(Roles = "Student")]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentStudentDto>> GetCurrentStudent(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();
        var student = await context.Students.AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => new CurrentStudentDto(s.Id, s.Name, s.RegistrationNumber, s.GPA, s.IsActive))
            .FirstOrDefaultAsync(ct);
        return student is null ? NotFound() : Ok(student);
    }
}
