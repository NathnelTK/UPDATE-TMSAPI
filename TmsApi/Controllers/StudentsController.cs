using Microsoft.AspNetCore.Mvc;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    // GET /api/students/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var student = await _studentService.GetByIdAsync(id);
        return student is not null ? Ok(student) : NotFound();
    }

    // GET /api/students/all
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var all = await _studentService.GetAllAsync();
        return Ok(all);
    }
}
