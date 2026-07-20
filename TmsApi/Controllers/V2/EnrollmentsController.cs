using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Enrollments.Commands;
using TmsApi.Enrollments.Queries;

namespace TmsApi.Controllers.V2;

/// <summary>
/// V2 Enrollments controller — uses CQRS with MediatR dispatch.
/// No business logic in the controller — only HTTP-status mapping from Result.Match().
/// POST /api/v2/enrollments — flat write with studentId and courseCode in body.
/// GET /api/v2/enrollments/{studentId}/schedule — student schedule query.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/enrollments")]
[ApiVersion("2.0")]
public class EnrollmentsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// POST /api/v2/enrollments — enroll a student by course code.
    /// Maps Result<T,E> to correct HTTP status codes via Match().
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Enroll(
        EnrollStudentCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: created => CreatedAtAction(
                nameof(GetSchedule),
                new { studentId = created.StudentId },
                created),
            onFailure: error =>
            {
                var status = error.Code switch
                {
                    "course_not_found" => StatusCodes.Status404NotFound,
                    "course_full" or "already_enrolled" => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status400BadRequest
                };

                return Problem(
                    statusCode: status,
                    title: "Enrollment rejected",
                    detail: error.Message,
                    type: $"https://tms.local/errors/{error.Code}");
            });
    }

    /// <summary>
    /// GET /api/v2/enrollments/{studentId}/schedule — get a student's enrolled courses.
    /// </summary>
    [HttpGet("{studentId}/schedule")]
    public async Task<IActionResult> GetSchedule(
        int studentId, CancellationToken ct)
    {
        var schedule = await mediator.Send(
            new GetStudentScheduleQuery(studentId), ct);
        return Ok(schedule);
    }
}