using System.Collections.Concurrent;

namespace TmsApi.Api.Transcripts;

public class StatusStore
{
    private readonly ConcurrentDictionary<string, TranscriptRequest> _requestsById = new();
    private readonly ConcurrentDictionary<string, string> _idempotencyKeyToReportId = new();
    private readonly ILogger<StatusStore> _logger;

    public StatusStore(ILogger<StatusStore> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Enqueues a new transcript request. If the idempotency key already exists,
    /// returns the existing ReportId without creating a duplicate.
    /// </summary>
    public string EnqueueRequest(string studentId, string idempotencyKey)
    {
        // Check for existing idempotency key first (idempotency guarantee)
        if (_idempotencyKeyToReportId.TryGetValue(idempotencyKey, out var existingReportId))
        {
            return existingReportId;
        }

        var reportId = Guid.NewGuid().ToString();

        var request = new TranscriptRequest
        {
            ReportId = reportId,
            StudentId = studentId,
            IdempotencyKey = idempotencyKey,
            Status = TranscriptStatus.Queued,
            QueuedAt = DateTime.UtcNow
        };

        // Attempt to add the idempotency key mapping atomically; if another thread
        // beat us to it, discard our new request and return the winner's ReportId.
        var addedReportId = _idempotencyKeyToReportId.GetOrAdd(idempotencyKey, reportId);
        if (addedReportId != reportId)
        {
            // Another thread won the race — return its ReportId
            return addedReportId;
        }

        // We own the idempotency key; store the request.
        _requestsById[reportId] = request;
        return reportId;
    }

    /// <summary>
    /// Returns the ReportId associated with the given idempotency key, or null if not found.
    /// </summary>
    public string? GetReportIdByIdempotencyKey(string key)
    {
        _idempotencyKeyToReportId.TryGetValue(key, out var reportId);
        return reportId;
    }

    /// <summary>
    /// Returns the TranscriptRequest with the given ReportId, or null if not found.
    /// </summary>
    public TranscriptRequest? GetRequestById(string reportId)
    {
        _requestsById.TryGetValue(reportId, out var request);
        return request;
    }

    /// <summary>
    /// Returns the oldest (by QueuedAt) request currently in Queued state, or null if none exist.
    /// </summary>
    public TranscriptRequest? GetNextQueuedRequest()
    {
        return _requestsById.Values
            .Where(r => r.Status == TranscriptStatus.Queued)
            .OrderBy(r => r.QueuedAt)
            .FirstOrDefault();
    }

    /// <summary>
    /// Attempts to transition the specified request to the target state.
    /// Valid transitions: Queued→Processing, Processing→Ready, Processing→Failed.
    /// Returns false (and logs a warning) if the transition is invalid or the ReportId is not found.
    /// </summary>
    public bool TryTransitionState(
        string reportId,
        TranscriptStatus target,
        string? downloadUrl = null,
        string? errorMessage = null)
    {
        if (!_requestsById.TryGetValue(reportId, out var request))
        {
            return false;
        }

        var isValid = (request.Status, target) switch
        {
            (TranscriptStatus.Queued, TranscriptStatus.Processing) => true,
            (TranscriptStatus.Processing, TranscriptStatus.Ready) => true,
            (TranscriptStatus.Processing, TranscriptStatus.Failed) => true,
            _ => false
        };

        if (!isValid)
        {
            _logger.LogWarning(
                "Invalid state transition rejected: ReportId={ReportId} from {CurrentState} to {TargetState}",
                reportId, request.Status, target);
            return false;
        }

        // Apply transition
        request.Status = target;

        switch (target)
        {
            case TranscriptStatus.Processing:
                request.ProcessingAt = DateTime.UtcNow;
                break;

            case TranscriptStatus.Ready:
                request.CompletedAt = DateTime.UtcNow;
                request.DownloadUrl = downloadUrl;
                break;

            case TranscriptStatus.Failed:
                request.FailedAt = DateTime.UtcNow;
                request.ErrorMessage = errorMessage;
                break;
        }

        return true;
    }
}
