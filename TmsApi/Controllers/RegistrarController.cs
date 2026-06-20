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

    // ========================================================================
    // Session 2 - Exercise 3: GroupBy, aggregates, and pagination
    // ========================================================================

    /// <summary>
    /// TODO 1: Paged list of students.
    /// Implements stable pagination with OrderBy before Skip/Take.
    /// SQL: SELECT ... FROM "Students" ORDER BY "Name" LIMIT @pageSize OFFSET @offset
    /// </summary>
    [HttpGet("students/paged")]
    public async Task<IActionResult> GetPagedStudents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100; // Cap page size to prevent abuse

        // Always OrderBy before Skip/Take for stable sorting
        var students = await context.Students
            .OrderBy(s => s.Name)                          // Stable sort required before Skip/Take
            .Skip((page - 1) * pageSize)                   // Offset: skip previous pages
            .Take(pageSize)                                // Limit: take only the requested page size
            .ToListAsync(cancellationToken);               // Materialize with cancellation support

        // Also get total count for client-side pagination UI
        var totalCount = await context.Students
            .CountAsync(cancellationToken);

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Students = students
        });
    }

    /// <summary>
    /// TODO 2: Top 5 courses by enrollment count.
    /// SQL: SELECT c."Title", COUNT(e."Id") FROM "Courses" c
    ///      LEFT JOIN "Enrollments" e ON c."Id" = e."CourseId"
    ///      GROUP BY c."Id", c."Title"
    ///      ORDER BY COUNT(e."Id") DESC
    ///      LIMIT 5
    /// </summary>
    [HttpGet("queries/top-courses")]
    public async Task<IActionResult> TopCoursesByEnrollment(
        CancellationToken cancellationToken = default)
    {
        var topCourses = await context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count   // EF translates this to SQL COUNT
            })
            .OrderByDescending(x => x.EnrollmentCount)  // Sort by count descending
            .Take(5)                                     // Only top 5
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            Description = "Top 5 courses by enrollment count",
            Results = topCourses
        });
    }
}