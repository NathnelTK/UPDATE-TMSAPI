namespace TmsApi.Api.Transcripts;

public record TranscriptStatusResponseDto
{
    public string ReportId { get; init; } = string.Empty;
    public string StudentId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty; // "Queued" | "Processing" | "Ready" | "Failed"
    public DateTime QueuedAt { get; init; }
    public DateTime? ProcessingAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? FailedAt { get; init; }
    public string? DownloadUrl { get; init; }   // Present when Status == "Ready"
    public string? ErrorMessage { get; init; }  // Present when Status == "Failed"
}
