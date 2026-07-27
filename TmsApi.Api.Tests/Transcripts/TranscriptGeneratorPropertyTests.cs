using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using TmsApi.Api.Transcripts;

namespace TmsApi.Api.Tests.Transcripts;

/// <summary>
/// Property-based tests for TranscriptGenerator processing logic (task 7.2–7.3).
/// Tests simulate the state-machine paths that TranscriptGenerator.ProcessRequestAsync
/// walks through, by driving StatusStore directly.
/// </summary>
public class TranscriptGeneratorPropertyTests
{
    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    private static StatusStore CreateStore() =>
        new StatusStore(NullLogger<StatusStore>.Instance);

    // -------------------------------------------------------------------
    // Task 7.2 — Property 5: Successful Processing Produces Ready State
    // Validates: Requirements 3.4
    // -------------------------------------------------------------------

    /// <summary>
    /// For any transcript request that completes the successful processing path,
    /// the final state MUST be Ready, DownloadUrl MUST be non-null,
    /// and CompletedAt MUST be non-null.
    ///
    /// Simulates what TranscriptGenerator.ProcessRequestAsync does on success:
    ///   1. Enqueue request (Queued state)
    ///   2. TryTransitionState → Processing  (worker picks it up)
    ///   3. TryTransitionState → Ready with downloadUrl  (generation succeeded)
    ///   4. Verify final state
    /// </summary>
    /// **Validates: Requirements 3.4**
    [Property]
    public Property Property5_SuccessfulProcessingProducesReadyState(NonEmptyString studentId)
    {
        // **Validates: Requirements 3.4**
        var store    = CreateStore();
        var reportId = store.EnqueueRequest(studentId.Get, Guid.NewGuid().ToString());

        // Step 2: simulate worker picking up the request
        var toProcessing = store.TryTransitionState(reportId, TranscriptStatus.Processing);

        // Step 3: simulate successful PDF generation
        var downloadUrl = $"https://cdn.tms.local/transcripts/{reportId}.pdf";
        var toReady     = store.TryTransitionState(reportId, TranscriptStatus.Ready, downloadUrl: downloadUrl);

        var request = store.GetRequestById(reportId)!;

        return (
            toProcessing              // Queued → Processing succeeded
            && toReady                // Processing → Ready succeeded
            && request.Status        == TranscriptStatus.Ready
            && request.DownloadUrl   != null
            && request.CompletedAt   != null
        ).ToProperty();
    }
}
