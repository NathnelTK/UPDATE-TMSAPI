using Microsoft.Extensions.Logging.Abstractions;
using TmsApi.Api.Transcripts;

namespace TmsApi.Api.Tests.Transcripts;

/// <summary>
/// Unit tests for StatusStore (task 2.7).
/// Requirements: 2.1–2.5, 4.1–4.10
/// </summary>
public class StatusStoreUnitTests
{
    private static StatusStore CreateStore() =>
        new StatusStore(NullLogger<StatusStore>.Instance);

    // -------------------------------------------------------------------
    // GetNextQueuedRequest — FIFO ordering
    // -------------------------------------------------------------------

    /// <summary>
    /// Enqueue three requests with a small guaranteed delay between them;
    /// GetNextQueuedRequest must return the one with the smallest QueuedAt.
    /// </summary>
    [Fact]
    public async Task GetNextQueuedRequest_ReturnsOldestQueuedRequest_FIFO()
    {
        var store = CreateStore();

        // Enqueue with slight delays to ensure distinct QueuedAt values.
        var id1 = store.EnqueueRequest("S-001", "key-fifo-1");
        await Task.Delay(5);
        var id2 = store.EnqueueRequest("S-002", "key-fifo-2");
        await Task.Delay(5);
        var id3 = store.EnqueueRequest("S-003", "key-fifo-3");

        var next = store.GetNextQueuedRequest();

        Assert.NotNull(next);
        Assert.Equal(id1, next.ReportId);
    }

    /// <summary>
    /// After the first queued request is transitioned away from Queued,
    /// GetNextQueuedRequest should return the second-oldest.
    /// </summary>
    [Fact]
    public async Task GetNextQueuedRequest_SkipsNonQueuedRequests_ReturnsNextOldest()
    {
        var store = CreateStore();

        var id1 = store.EnqueueRequest("S-001", "key-order-1");
        await Task.Delay(5);
        var id2 = store.EnqueueRequest("S-002", "key-order-2");

        // Transition id1 away from Queued.
        store.TryTransitionState(id1, TranscriptStatus.Processing);

        var next = store.GetNextQueuedRequest();

        Assert.NotNull(next);
        Assert.Equal(id2, next.ReportId);
    }

    /// <summary>
    /// When no requests are in Queued state, GetNextQueuedRequest returns null.
    /// </summary>
    [Fact]
    public void GetNextQueuedRequest_WhenNoQueuedRequests_ReturnsNull()
    {
        var store = CreateStore();
        Assert.Null(store.GetNextQueuedRequest());
    }

    // -------------------------------------------------------------------
    // GetReportIdByIdempotencyKey — unknown key returns null
    // -------------------------------------------------------------------

    [Fact]
    public void GetReportIdByIdempotencyKey_UnknownKey_ReturnsNull()
    {
        var store = CreateStore();
        var result = store.GetReportIdByIdempotencyKey("nonexistent-key");
        Assert.Null(result);
    }

    [Fact]
    public void GetReportIdByIdempotencyKey_KnownKey_ReturnsCorrectReportId()
    {
        var store = CreateStore();
        var reportId = store.EnqueueRequest("S-010", "my-unique-key");

        var retrieved = store.GetReportIdByIdempotencyKey("my-unique-key");
        Assert.Equal(reportId, retrieved);
    }

    // -------------------------------------------------------------------
    // TryTransitionState — non-existent ReportId returns false
    // -------------------------------------------------------------------

    [Fact]
    public void TryTransitionState_NonExistentReportId_ReturnsFalse()
    {
        var store = CreateStore();
        var result = store.TryTransitionState("does-not-exist", TranscriptStatus.Processing);
        Assert.False(result);
    }

    // -------------------------------------------------------------------
    // EnqueueRequest — returns existing id for duplicate key
    // -------------------------------------------------------------------

    [Fact]
    public void EnqueueRequest_DuplicateKey_ReturnsSameReportId()
    {
        var store = CreateStore();
        var id1 = store.EnqueueRequest("S-001", "dup-key");
        var id2 = store.EnqueueRequest("S-001", "dup-key");
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void EnqueueRequest_NewRequest_StatusIsQueued()
    {
        var store = CreateStore();
        var reportId = store.EnqueueRequest("S-001", "brand-new-key");
        var request  = store.GetRequestById(reportId);

        Assert.NotNull(request);
        Assert.Equal(TranscriptStatus.Queued, request.Status);
        Assert.Equal("S-001", request.StudentId);
    }
}
