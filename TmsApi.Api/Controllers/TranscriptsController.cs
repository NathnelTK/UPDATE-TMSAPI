using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Api.Transcripts;

namespace TmsApi.Api.Controllers;

/// <summary>
/// Controller for transcript operations.
/// Uses concurrency limiter to prevent too many simultaneous transcript generations.
/// </summary>
[ApiController]
[Route("api/v2/transcripts")]
public class TranscriptsController(StatusStore statusStore, ILogger<TranscriptsController> logger)
    : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transcripts")]
    public IActionResult RequestTranscript(
        [FromBody] TranscriptRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            return BadRequest(new { error = "Idempotency-Key header required" });
        }

        // Check for duplicate idempotency key (Req 2.1)
        var existingReportId = statusStore.GetReportIdByIdempotencyKey(idempotencyKey);
        if (existingReportId is not null)
        {
            logger.LogInformation(
                "Duplicate request detected: IdempotencyKey={Key}, ReportId={ReportId}",
                idempotencyKey, existingReportId);

            var duplicateLocationUrl = Url?.Action(
                nameof(GetStatus),
                null,
                new { reportId = existingReportId },
                Request?.Scheme)
                ?? $"/api/v2/transcripts/{existingReportId}/status";

            return Accepted(duplicateLocationUrl, new { reportId = existingReportId, duplicate = true });
        }

        // New unique key — enqueue the request (Req 1.4, 2.4)
        var reportId = statusStore.EnqueueRequest(request.StudentId, idempotencyKey);

        // Req 16.1 — log ReportId and key on enqueue
        logger.LogInformation(
            "Transcript request enqueued: ReportId={ReportId}, IdempotencyKey={Key}",
            reportId, idempotencyKey);

        var locationUrl = Url?.Action(
            nameof(GetStatus),
            null,
            new { reportId },
            Request?.Scheme)
            ?? $"/api/v2/transcripts/{reportId}/status";

        // Req 1.1–1.3: return 202 Accepted immediately with Location header and reportId body
        return Accepted(locationUrl, new { reportId });
    }

    [HttpGet("{reportId}/status")]
    public IActionResult GetStatus(string reportId)
    {
        var transcriptRequest = statusStore.GetRequestById(reportId);

        if (transcriptRequest is null)
        {
            return NotFound();
        }

        // Map TranscriptRequest → TranscriptStatusResponseDto (Req 5.1–5.5, 15.5)
        var dto = new TranscriptStatusResponseDto
        {
            ReportId = transcriptRequest.ReportId,
            StudentId = transcriptRequest.StudentId,
            Status = transcriptRequest.Status.ToString(),
            QueuedAt = transcriptRequest.QueuedAt,
            ProcessingAt = transcriptRequest.ProcessingAt,
            CompletedAt = transcriptRequest.CompletedAt,
            FailedAt = transcriptRequest.FailedAt,
            // Include downloadUrl only when Ready (Req 5.3)
            DownloadUrl = transcriptRequest.Status == TranscriptStatus.Ready
                ? transcriptRequest.DownloadUrl
                : null,
            // Include errorMessage only when Failed (Req 5.4)
            ErrorMessage = transcriptRequest.Status == TranscriptStatus.Failed
                ? transcriptRequest.ErrorMessage
                : null
        };

        return Ok(dto);
    }
}
