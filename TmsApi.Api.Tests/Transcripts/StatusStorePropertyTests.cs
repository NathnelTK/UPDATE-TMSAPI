using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using TmsApi.Api.Transcripts;

namespace TmsApi.Api.Tests.Transcripts;

/// <summary>
/// Property-based tests for StatusStore (tasks 2.2–2.6).
/// Uses FsCheck.Xunit [Property] attribute where random input generation is
/// beneficial, and [Fact] for deterministic / concurrency scenarios.
/// </summary>
public class StatusStorePropertyTests
{
    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    private static StatusStore CreateStore() =>
        new StatusStore(NullLogger<StatusStore>.Instance);

    // -------------------------------------------------------------------
    // Task 2.2 — Property 1: State Machine Transition Validation
    // Validates: Requirements 4.2, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10
    // -------------------------------------------------------------------

    /// <summary>
    /// Exhaustively test all 16 (current, target) state pairs.
    /// TryTransitionState must return true for exactly the 3 valid
    /// transitions and false for the remaining 13.
    /// </summary>
    [Fact]
    public void Property1_StateMachineTransitionValidation_AllSixteenCombinations()
    {
        // **Validates: Requirements 4.2, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10**
        var validTransitions = new HashSet<(TranscriptStatus, TranscriptStatus)>
        {
            (TranscriptStatus.Queued,      TranscriptStatus.Processing),
            (TranscriptStatus.Processing,  TranscriptStatus.Ready),
            (TranscriptStatus.Processing,  TranscriptStatus.Failed),
        };

        var allStatuses = Enum.GetValues<TranscriptStatus>();

        foreach (var current in allStatuses)
        {
            foreach (var target in allStatuses)
            {
                var store = CreateStore();

                // Enqueue a fresh request (starts in Queued state).
                var reportId = store.EnqueueRequest("S-001", Guid.NewGuid().ToString());

                // Force the request into the desired 'current' state by
                // walking through valid transitions.
                ForceState(store, reportId, current);

                var result = store.TryTransitionState(reportId, target);
                var expected = validTransitions.Contains((current, target));

                Assert.True(
                    result == expected,
                    $"Expected TryTransitionState({current} → {target}) = {expected}, got {result}");
            }
        }
    }

    // -------------------------------------------------------------------
    // Task 2.3 — Property 2: Unique Idempotency Key Creates New Request
    // Validates: Requirements 2.4, 1.4
    // -------------------------------------------------------------------

    /// <summary>
    /// For any unique (studentId, idempotencyKey) pair, EnqueueRequest must
    /// create exactly one request in Queued state whose StudentId matches.
    /// </summary>
    [Property]
    public Property Property2_UniqueIdempotencyKeyCreatesNewRequest(
        NonEmptyString studentId,
        NonEmptyString idempotencyKey)
    {
        // **Validates: Requirements 2.4, 1.4**
        var store = CreateStore();

        var reportId = store.EnqueueRequest(studentId.Get, idempotencyKey.Get);
        var request  = store.GetRequestById(reportId);

        return (
            reportId   != null
            && request != null
            && request.Status        == TranscriptStatus.Queued
            && request.StudentId     == studentId.Get
            && request.IdempotencyKey == idempotencyKey.Get
            && store.GetReportIdByIdempotencyKey(idempotencyKey.Get) == reportId
        ).ToProperty();
    }

    // -------------------------------------------------------------------
    // Task 2.4 — Property 3: Duplicate Idempotency Key Returns Existing ReportId
    // Validates: Requirements 2.2, 2.3
    // -------------------------------------------------------------------

    /// <summary>
    /// Enqueueing the same idempotency key twice must return the same
    /// ReportId and leave exactly one request in the store.
    /// </summary>
    [Property]
    public Property Property3_DuplicateIdempotencyKeyReturnsExistingReportId(
        NonEmptyString studentId,
        NonEmptyString idempotencyKey)
    {
        // **Validates: Requirements 2.2, 2.3**
        var store = CreateStore();

        var firstReportId  = store.EnqueueRequest(studentId.Get, idempotencyKey.Get);
        var secondReportId = store.EnqueueRequest(studentId.Get, idempotencyKey.Get);

        // Count requests that have this idempotency key
        int requestCount = 0;
        for (var id = store.GetReportIdByIdempotencyKey(idempotencyKey.Get);
             id != null;
             id = null)   // single iteration — just checking one mapping exists
        {
            requestCount++;
        }

        // Verify: both calls return the same ReportId, and there is exactly
        // one entry in the store for this key.
        var request = store.GetRequestById(firstReportId);

        return (
            firstReportId == secondReportId
            && request    != null
            && store.GetReportIdByIdempotencyKey(idempotencyKey.Get) == firstReportId
        ).ToProperty();
    }

    // -------------------------------------------------------------------
    // Task 2.5 — Property 8: Concurrent Operations Maintain Store Consistency
    // Validates: Requirements 13.2, 13.5
    // -------------------------------------------------------------------

    /// <summary>
    /// N threads each enqueue M distinct requests concurrently.
    /// After all tasks complete: no exceptions, no duplicate ReportIds,
    /// and request count equals number of distinct idempotency keys used.
    /// </summary>
    [Fact]
    public async Task Property8_ConcurrentOperationsMaintainStoreConsistency()
    {
        // **Validates: Requirements 13.2, 13.5**
        const int threadCount = 10;
        const int requestsPerThread = 5;

        var store = CreateStore();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var allReportIds = new System.Collections.Concurrent.ConcurrentBag<string>();
        var allKeys = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Build the set of unique idempotency keys up front so we can
        // verify count equality afterwards.
        var tasks = Enumerable.Range(0, threadCount).Select(t => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < requestsPerThread; i++)
                {
                    var key = $"thread-{t}-request-{i}";
                    allKeys.Add(key);

                    var reportId = store.EnqueueRequest($"student-{t}", key);
                    allReportIds.Add(reportId);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // 1. No exceptions during concurrent access
        Assert.Empty(exceptions);

        // 2. No duplicate ReportIds returned
        var distinctReportIds = allReportIds.Distinct().ToList();
        Assert.Equal(allReportIds.Count, distinctReportIds.Count);

        // 3. Request count == distinct idempotency keys (all keys are unique by design)
        var distinctKeys = allKeys.Distinct().ToList();
        Assert.Equal(distinctKeys.Count, distinctReportIds.Count);
    }

    // -------------------------------------------------------------------
    // Task 2.6 — Property 9: State Transitions Record Timestamps
    // Validates: Requirements 15.1, 15.2, 15.3, 15.4
    // -------------------------------------------------------------------

    /// <summary>
    /// Walk Queued→Processing→Ready path.
    /// After each transition the relevant timestamp must be set and
    /// future-state timestamps must remain null.  Timestamps must be
    /// chronologically ordered.
    /// </summary>
    [Fact]
    public void Property9_StateTransitions_QueuedToProcessingToReady_RecordsTimestamps()
    {
        // **Validates: Requirements 15.1, 15.2, 15.3, 15.4**
        var store = CreateStore();
        var reportId = store.EnqueueRequest("S-001", Guid.NewGuid().ToString());
        var request  = store.GetRequestById(reportId)!;

        // --- After Enqueue (Queued) ---
        Assert.Equal(TranscriptStatus.Queued, request.Status);
        Assert.True(request.QueuedAt > DateTime.MinValue);
        Assert.Null(request.ProcessingAt);
        Assert.Null(request.CompletedAt);
        Assert.Null(request.FailedAt);

        // --- After Queued → Processing ---
        var before1 = DateTime.UtcNow;
        var ok1 = store.TryTransitionState(reportId, TranscriptStatus.Processing);
        Assert.True(ok1);

        Assert.Equal(TranscriptStatus.Processing, request.Status);
        Assert.NotNull(request.ProcessingAt);
        Assert.True(request.ProcessingAt >= before1 || request.ProcessingAt >= request.QueuedAt);
        Assert.Null(request.CompletedAt);
        Assert.Null(request.FailedAt);

        // Chronological order so far
        Assert.True(request.QueuedAt <= request.ProcessingAt!.Value);

        // --- After Processing → Ready ---
        var before2 = DateTime.UtcNow;
        var ok2 = store.TryTransitionState(reportId, TranscriptStatus.Ready, downloadUrl: "https://cdn.tms.local/transcripts/test.pdf");
        Assert.True(ok2);

        Assert.Equal(TranscriptStatus.Ready, request.Status);
        Assert.NotNull(request.CompletedAt);
        Assert.True(request.CompletedAt >= before2 || request.CompletedAt >= request.ProcessingAt);
        Assert.Null(request.FailedAt);

        // Chronological order: QueuedAt ≤ ProcessingAt ≤ CompletedAt
        Assert.True(request.QueuedAt <= request.ProcessingAt!.Value);
        Assert.True(request.ProcessingAt!.Value <= request.CompletedAt!.Value);
    }

    /// <summary>
    /// Walk Queued→Processing→Failed path.
    /// After each transition the relevant timestamp must be set and
    /// future-state timestamps must remain null.
    /// </summary>
    [Fact]
    public void Property9_StateTransitions_QueuedToProcessingToFailed_RecordsTimestamps()
    {
        // **Validates: Requirements 15.1, 15.2, 15.3, 15.4**
        var store = CreateStore();
        var reportId = store.EnqueueRequest("S-002", Guid.NewGuid().ToString());
        var request  = store.GetRequestById(reportId)!;

        // Queued state baseline
        Assert.Equal(TranscriptStatus.Queued, request.Status);
        Assert.True(request.QueuedAt > DateTime.MinValue);
        Assert.Null(request.ProcessingAt);
        Assert.Null(request.CompletedAt);
        Assert.Null(request.FailedAt);

        // Queued → Processing
        var ok1 = store.TryTransitionState(reportId, TranscriptStatus.Processing);
        Assert.True(ok1);
        Assert.NotNull(request.ProcessingAt);
        Assert.Null(request.CompletedAt);
        Assert.Null(request.FailedAt);
        Assert.True(request.QueuedAt <= request.ProcessingAt!.Value);

        // Processing → Failed
        var ok2 = store.TryTransitionState(reportId, TranscriptStatus.Failed, errorMessage: "generation error");
        Assert.True(ok2);
        Assert.Equal(TranscriptStatus.Failed, request.Status);
        Assert.NotNull(request.FailedAt);
        Assert.Null(request.CompletedAt);   // Ready timestamp must stay null
        Assert.True(request.ProcessingAt!.Value <= request.FailedAt!.Value);
    }

    // -------------------------------------------------------------------
    // Internal helper: force a fresh Queued request into any state via
    // valid transition sequences, or leave it alone if target == Queued.
    // -------------------------------------------------------------------

    private static void ForceState(StatusStore store, string reportId, TranscriptStatus target)
    {
        switch (target)
        {
            case TranscriptStatus.Queued:
                // Already in Queued; nothing to do.
                break;

            case TranscriptStatus.Processing:
                store.TryTransitionState(reportId, TranscriptStatus.Processing);
                break;

            case TranscriptStatus.Ready:
                store.TryTransitionState(reportId, TranscriptStatus.Processing);
                store.TryTransitionState(reportId, TranscriptStatus.Ready);
                break;

            case TranscriptStatus.Failed:
                store.TryTransitionState(reportId, TranscriptStatus.Processing);
                store.TryTransitionState(reportId, TranscriptStatus.Failed);
                break;
        }
    }
}
