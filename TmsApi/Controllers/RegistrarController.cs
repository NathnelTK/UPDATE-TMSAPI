using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

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
    /// Paged list of students.
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
    /// Top 5 courses by enrollment count.
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

    // ========================================================================
    // Session 3 - Exercise 7: N+1 query problem demonstration and fix
    // ========================================================================

    /// <summary>
    /// PART A: Intentional N+1 — loads students, then queries enrollment count
    /// per student inside a loop. Produces 1 + N SQL statements.
    /// </summary>
    [HttpGet("n-plus-one/before")]
    public async Task<IActionResult> NPlusOneBefore(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("\n>>> N+1 DEMO (BEFORE): Loading students then querying inside loop...\n");

        // Step 1: Load all students (1 query)
        var students = await context.Students
            .AsNoTracking()
            .IgnoreQueryFilters() // Include soft-deleted students for demo
            .ToListAsync(cancellationToken);

        var results = new List<object>();

        // Step 2: For each student, query enrollments inside the loop (N queries)
        foreach (var s in students)
        {
            var count = await context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id, cancellationToken);

            Console.WriteLine($"{s.Name}: {count} enrollments");
            results.Add(new { s.Name, EnrollmentCount = count });
        }

        Console.WriteLine($"\n>>> Total SQL statements: 1 (students) + {students.Count} (enrollment counts) = {1 + students.Count}\n");

        return Ok(new
        {
            Description = $"N+1 demo: 1 query for students + {students.Count} queries for enrollments = {1 + students.Count} total SQL statements",
            Results = results
        });
    }

    /// <summary>
    /// PART B: Fix with shaping — single query with projection.
    /// EF translates s.Enrollments.Count into a SQL subquery.
    /// </summary>
    [HttpGet("n-plus-one/after")]
    public async Task<IActionResult> NPlusOneAfter(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("\n>>> N+1 DEMO (AFTER): Single query with projection...\n");

        // Single query with projection — EF translates to JOIN + COUNT subquery
        var report = await context.Students
            .AsNoTracking()
            .IgnoreQueryFilters() // Include soft-deleted students for demo
            .Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count // Translated to SQL COUNT subquery
            })
            .ToListAsync(cancellationToken);

        foreach (var r in report)
            Console.WriteLine($"{r.Name}: {r.EnrollmentCount} enrollments");

        Console.WriteLine("\n>>> Only 1 SQL statement executed (with correlated subquery)\n");

        return Ok(new
        {
            Description = "N+1 fix: single query with projection — EF translates to SQL subquery",
            Results = report
        });
    }

    /// <summary>
    /// Alternative N+1 fix using Include (loads full enrollment objects).
    /// Heavier but sometimes useful.
    /// </summary>
    [HttpGet("n-plus-one/include")]
    public async Task<IActionResult> NPlusOneInclude(CancellationToken cancellationToken = default)
    {
        var students = await context.Students
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(s => s.Enrollments) // Eager load enrollments
            .ToListAsync(cancellationToken);

        var results = students.Select(s => new
        {
            s.Name,
            EnrollmentCount = s.Enrollments.Count
        }).ToList();

        return Ok(new
        {
            Description = "N+1 alternative fix using Include — eager loads all enrollments in one query",
            Results = results
        });
    }

    // ========================================================================
    // Session 3 - Exercise 8: Concurrency test endpoint
    // ========================================================================

    /// <summary>
    /// Load a student by ID for concurrency testing.
    /// Returns the student data including the current Version (xmin).
    /// </summary>
    [HttpGet("students/{id:int}")]
    public async Task<IActionResult> GetStudent(int id, CancellationToken cancellationToken = default)
    {
        var student = await context.Students
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (student is null)
            return NotFound(new { Message = $"Student with ID {id} not found" });

        return Ok(new
        {
            student.Id,
            student.Name,
            student.GPA,
            student.RegistrationNumber,
            student.IsActive,
            student.IsDeleted,
            Version = student.Version // xmin concurrency token
        });
    }

    /// <summary>
    /// Update a student's GPA — used to test concurrency.
    /// Will throw DbUpdateConcurrencyException if the Version doesn't match.
    /// </summary>
    [HttpPut("students/{id:int}/update-gpa")]
    public async Task<IActionResult> UpdateStudentGpa(
        int id,
        [FromBody] UpdateGpaRequest request,
        CancellationToken cancellationToken = default)
    {
        var student = await context.Students
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (student is null)
            return NotFound(new { Message = $"Student with ID {id} not found" });

        // Update the shadow property LastUpdated before saving
        context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

        student.GPA = request.NewGpa;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Conflict(new
            {
                Message = "Concurrency conflict: another user updated this record before you.",
                Detail = ex.Message
            });
        }

        return Ok(new
        {
            Message = "GPA updated successfully",
            student.Id,
            student.Name,
            student.GPA,
            student.Version
        });
    }

    // ========================================================================
    // Session 3 - Exercise 9: Bulk archive and soft-delete operations
    // ========================================================================

    /// <summary>
    /// Bulk archive enrollments older than a cutoff date.
    /// Uses ExecuteUpdateAsync to perform set-based UPDATE in a single SQL statement.
    /// </summary>
    [HttpPost("enrollments/archive-old")]
    public async Task<IActionResult> ArchiveOldEnrollments(
        [FromBody] ArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var cutoff = request.CutoffDate ?? DateTime.UtcNow.AddMonths(-6);

        Console.WriteLine($"\n>>> BULK ARCHIVE: Archiving enrollments older than {cutoff:yyyy-MM-dd}...\n");

        var affected = await context.Enrollments
            .Where(e => e.EnrolledAt < cutoff)
            .ExecuteUpdateAsync(
                s => s.SetProperty(e => e.IsArchived, true),
                cancellationToken);

        Console.WriteLine($">>> {affected} enrollments archived in a single SQL UPDATE statement.\n");

        return Ok(new
        {
            Message = $"Bulk archive completed",
            ArchivedCount = affected,
            CutoffDate = cutoff,
            SqlStatement = $"UPDATE \"Enrollments\" SET \"IsArchived\" = true WHERE \"EnrolledAt\" < '{cutoff:yyyy-MM-dd}'"
        });
    }

    /// <summary>
    /// Soft-delete a student (set IsDeleted = true).
    /// After this, the HasQueryFilter will exclude them from normal queries.
    /// </summary>
    [HttpPost("students/{id:int}/soft-delete")]
    public async Task<IActionResult> SoftDeleteStudent(int id, CancellationToken cancellationToken = default)
    {
        var student = await context.Students
            .IgnoreQueryFilters() // Need to bypass filter to find already-soft-deleted too
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (student is null)
            return NotFound(new { Message = $"Student with ID {id} not found" });

        student.IsDeleted = true;

        // Update the shadow property
        context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            Message = $"Student '{student.Name}' (ID {id}) soft-deleted. They will be hidden from normal queries.",
            student.Id,
            student.Name,
            student.IsDeleted
        });
    }

    /// <summary>
    /// Normal query — soft-deleted students are filtered out by HasQueryFilter.
    /// </summary>
    [HttpGet("students/active-list")]
    public async Task<IActionResult> GetActiveStudents(CancellationToken cancellationToken = default)
    {
        // This automatically applies the HasQueryFilter(s => !s.IsDeleted)
        var students = await context.Students
            .Select(s => new { s.Id, s.Name, s.RegistrationNumber, s.IsDeleted })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            Description = "Active students (soft-deleted excluded automatically by HasQueryFilter)",
            Count = students.Count,
            Students = students
        });
    }

    /// <summary>
    /// Admin query — uses IgnoreQueryFilters() to include soft-deleted students.
    /// </summary>
    [HttpGet("students/all-including-deleted")]
    public async Task<IActionResult> GetAllStudentsIncludingDeleted(CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters() bypasses the soft-delete filter for admin scenarios
        var students = await context.Students
            .IgnoreQueryFilters()
            .Select(s => new { s.Id, s.Name, s.RegistrationNumber, s.IsDeleted })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            Description = "All students including soft-deleted (admin override via IgnoreQueryFilters)",
            Count = students.Count,
            Students = students
        });
    }

    /// <summary>
    /// Restore a soft-deleted student.
    /// </summary>
    [HttpPost("students/{id:int}/restore")]
    public async Task<IActionResult> RestoreStudent(int id, CancellationToken cancellationToken = default)
    {
        var student = await context.Students
            .IgnoreQueryFilters() // Must bypass filter to find deleted students
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (student is null)
            return NotFound(new { Message = $"Student with ID {id} not found" });

        if (!student.IsDeleted)
            return BadRequest(new { Message = $"Student '{student.Name}' is not deleted." });

        student.IsDeleted = false;
        context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            Message = $"Student '{student.Name}' restored successfully.",
            student.Id,
            student.Name,
            student.IsDeleted
        });
    }
}

// ========================================================================
// Request DTOs
// ========================================================================

public class UpdateGpaRequest
{
    public decimal NewGpa { get; set; }
}

public class ArchiveRequest
{
    public DateTime? CutoffDate { get; set; }
}