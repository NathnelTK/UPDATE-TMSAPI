using Microsoft.AspNetCore.Mvc;

namespace TmsApi.Controllers;

// --- Session 3 - Exercise 5: Enrollments CRUD Controller ---
// Exposes CRUD REST endpoints at `/api/enrollments` to integrate with the frontend.
// Injects IEnrollmentService to interact with the underlying business logic.
// Utilizes proper REST semantics, such as 201 Created with Location header,
// 204 NoContent for successful deletion, and 404 NotFound for non-existent items.

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    // GET /api/enrollments -> returns all enrollment records
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var enrollments = await _enrollmentService.GetAllAsync();
        return Ok(enrollments);
    }

    // GET /api/enrollments/{id} -> returns one record or 404
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var record = await _enrollmentService.GetByIdAsync(id);
        return record is not null ? Ok(record) : NotFound();
    }

    // POST /api/enrollments -> creates a new enrollment and returns 201 Created with Location header
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request)
    {
        var record = await _enrollmentService.EnrollAsync(request.StudentId, request.CourseCode);
        
        // CreatedAtAction generates a 201 response status, sets the Location header pointing
        // to GetById, and outputs the record JSON in the response body.
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    // DELETE /api/enrollments/{id} -> returns 204 NoContent or 404 NotFound
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _enrollmentService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}

// Request payload model for creating an enrollment record
public record CreateEnrollmentRequest(string StudentId, string CourseCode);
