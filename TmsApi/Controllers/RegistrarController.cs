using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/registrar")]
public class RegistrarController(TmsDbContext context) : ControllerBase
{
    /// <summary>
    /// Query 1: How many active students have GPA >= 3.0?
    /// SQL: SELECT COUNT(*)::int4 FROM "Students" AS s WHERE s."IsActive" AND s."GPA" >= 3.0
    /// </summary>
    [HttpGet("queries/active-high-gpa-count")]
    public async Task<IActionResult> ActiveHighGpaCount()
    {
        var count = await context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();

        return Ok(new { Description = "Active students with GPA >= 3.0", Count = count });
    }

    /// <summary>
    /// Query 2: Which courses have the most enrollments, sorted descending?
    /// SQL: SELECT c."Title", COUNT(*) FROM "Enrollments" ... GROUP BY ... ORDER BY ...
    /// </summary>
    [HttpGet("queries/courses-by-enrollments")]
    public async Task<IActionResult> CoursesByEnrollments()
    {
        var list = await context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();

        return Ok(new { Description = "Courses sorted by enrollment count (descending)", Results = list });
    }

    /// <summary>
    /// Query 3: What is the average GPA per course?
    /// SQL: SELECT c."Title", AVG(s."GPA") FROM "Enrollments" ... GROUP BY ... 
    /// </summary>
    [HttpGet("queries/average-gpa-per-course")]
    public async Task<IActionResult> AverageGpaPerCourse()
    {
        var list = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                Course = g.Key,
                AverageGPA = g.Average(e => e.Student.GPA)
            })
            .ToListAsync();

        return Ok(new { Description = "Average GPA per course", Results = list });
    }

    /// <summary>
    /// Query 4: Which students have zero enrollments?
    /// Approach A: Using subquery (NOT EXISTS)
    /// SQL: SELECT s."Name" FROM "Students" AS s WHERE NOT EXISTS (SELECT 1 FROM "Enrollments" ...)
    /// </summary>
    [HttpGet("queries/students-no-enrollments/subquery")]
    public async Task<IActionResult> StudentsNoEnrollmentsSubquery()
    {
        var list = await context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();

        return Ok(new { Description = "Students with zero enrollments (subquery: NOT EXISTS)", Results = list });
    }

    /// <summary>
    /// Query 4 (Alternative): Which students have zero enrollments?
    /// Approach B: Using EF Core left join (LEFT JOIN ... WHERE ... IS NULL)
    /// </summary>
    [HttpGet("queries/students-no-enrollments/left-join")]
    public async Task<IActionResult> StudentsNoEnrollmentsLeftJoin()
    {
        var list = await context.Students
            .LeftJoin(context.Enrollments,
                s => s.Id,
                e => e.StudentId,
                (s, e) => new { s, e })
            .Where(x => x.e == null)
            .Select(x => x.s.Name)
            .ToListAsync();

        return Ok(new { Description = "Students with zero enrollments (LEFT JOIN ... IS NULL)", Results = list });
    }
}