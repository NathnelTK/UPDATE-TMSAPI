using Microsoft.AspNetCore.Mvc;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
{
    /// <summary>
    /// Experiment: demonstrates LINQ deferred execution.
    /// The query is built in steps but only hits the database when .ToList() is called.
    /// </summary>
    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building the query object (no database contact)...");
        var query = context.Students.Where(s => s.GPA >= 3.0m);

        Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
        var orderedQuery = query.OrderBy(s => s.Name);

        Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
        var results = orderedQuery.ToList(); // Execution is triggered here

        Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");

        return Ok(results);
    }

    // Non-translatable helper method
    private static bool IsHonorRoll(decimal gpa)
    {
        return gpa >= 3.5m;
    }

    /// <summary>
    /// Experiment: demonstrates that EF Core cannot translate arbitrary C# methods to SQL.
    /// This will throw an InvalidOperationException because IsHonorRoll is not translatable.
    /// </summary>
    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
        try
        {
            var students = context.Students
                .Where(s => IsHonorRoll(s.GPA)) // EF Core does not know how to map this method to SQL
                .ToList();
            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Server-side resolution: inline the logic so EF Core can translate it.
    /// </summary>
    [HttpGet("translation-resolved")]
    public IActionResult TestTranslationResolved()
    {
        var students = context.Students
            .Where(s => s.GPA >= 3.5m) // EF Core can translate this directly to SQL
            .ToList();

        return Ok(students);
    }

    /// <summary>
    /// Client-side evaluation: uses AsEnumerable() to pull data into memory first,
    /// then applies C# logic. Demonstrates the performance trade-off.
    /// </summary>
    [HttpGet("client-eval")]
    public IActionResult TestClientEval()
    {
        var students = context.Students
            .AsEnumerable() // Pulls all rows into application RAM
            .Where(s => IsHonorRoll(s.GPA))
            .ToList();

        return Ok(students);
    }
}