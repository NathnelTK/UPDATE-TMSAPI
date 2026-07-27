namespace TmsApi.Api.Transcripts;

public class TranscriptRequest
{
    public string ReportId { get; init; } = Guid.NewGuid().ToString();
    public string StudentId { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
    public TranscriptStatus Status { get; set; } = TranscriptStatus.Queued;
    public DateTime QueuedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessingAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? DownloadUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
