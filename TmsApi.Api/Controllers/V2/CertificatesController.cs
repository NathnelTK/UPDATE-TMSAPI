using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

// --- M7 Session 4 - Exercise 8: V2 Certificates controller ---
// Delegates to ICertificateService which wraps the external call in a Polly v8 pipeline.
// The controller maps InvalidOperationException (non-transient 4xx from downstream)
// to HTTP 400 without retry — the pass/fail signal for the lab test.
[ApiController]
[Route("api/v{version:apiVersion}/certificates")]
[ApiVersion("2.0")]
public sealed class CertificatesController(ICertificateService certificates) : ControllerBase
{
    public sealed record IssueRequest(int StudentId, string CourseCode);

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Issue([FromBody] IssueRequest req, CancellationToken ct)
    {
        try
        {
            var result = await certificates.IssueCertificateAsync(
                req.StudentId, req.CourseCode, ct);
            return Ok(result);
        }
        // 4xx from the fake certificate service → 400 to caller, no retry storm.
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Certificate request rejected",
                detail: ex.Message);
        }
    }
}
