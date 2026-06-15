using Microsoft.AspNetCore.Mvc;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    // GET /api/courses/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var course = await _courseService.GetByIdAsync(id);
        return course is not null ? Ok(course) : NotFound();
    }

    // GET /api/courses/all
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var all = await _courseService.GetAllAsync();
        return Ok(all);
    }
}
