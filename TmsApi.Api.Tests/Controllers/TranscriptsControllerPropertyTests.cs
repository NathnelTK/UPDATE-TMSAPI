using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using TmsApi.Api.Controllers;
using TmsApi.Api.Transcripts;

namespace TmsApi.Api.Tests.Controllers;

/// <summary>
/// Property-based tests for TranscriptsController (task 4.2).
/// Property 4: Status Query Returns Current State
/// Validates: Requirements 5.1
/// </summary>
public class TranscriptsControllerPropertyTests
{
    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    private static StatusStore CreateStore() =>
        new StatusStore(NullLogger<StatusStore>.Instance);

    private static TranscriptsController CreateController(StatusStore store) =>
        new TranscriptsController(store, NullLogger<TranscriptsController>.Instance);

    /// <summary>
    /// Walk a fresh Queued request through valid transitions until it reaches
    /// the requested target state.
    /// </summary>
    private static void ForceState(StatusStore store, string reportId, TranscriptStatus target)
    {
        switch (target)
        {
            case TranscriptStatus.Queued:
                // Already in Queued after EnqueueRequest; nothing to do.
                break;

            case TranscriptStatus.Processing:
                store.TryTransitionState(reportId, TranscriptStatus.Processing);
                break;

            case TranscriptStatus.Ready:
                store.TryTransitionState(reportId, TranscriptStatus.Processing);
                store.TryTransitionState(reportId, TranscriptStatus.Ready,
                    downloadUrl: "https://cdn.tms.local/transcripts/test.pdf");
                break;

            case TranscriptStatus.Failed:
                store.TryTransitionState(reportId, TranscriptStatus.Processing);
                store.TryTransitionState(reportId, TranscriptStatus.Failed,
                    errorMessage: "simulated failure");
                break;
        }
    }

    // -------------------------------------------------------------------
    // Task 4.2 — Property 4: Status Query Returns Current State
    // **Validates: Requirements 5.1**
    // -------------------------------------------------------------------

    /// <summary>
    /// For each of the four valid states (Queued, Processing, Ready, Failed):
    /// enqueue a fresh request, force it into the target state via valid
    /// transitions, call GetStatus, and verify the response Status string
    /// matches the enum name.
    /// </summary>
    [Theory]
    [InlineData(TranscriptStatus.Queued)]
    [InlineData(TranscriptStatus.Processing)]
    [InlineData(TranscriptStatus.Ready)]
    [InlineData(TranscriptStatus.Failed)]
    public void Property4_StatusQueryReturnsCurrentState(TranscriptStatus targetState)
    {
        // **Validates: Requirements 5.1**

        // Arrange
        var store      = CreateStore();
        var controller = CreateController(store);

        var reportId = store.EnqueueRequest("S-001", Guid.NewGuid().ToString());

        // Force the request into the target state via valid transitions
        ForceState(store, reportId, targetState);

        // Confirm the store actually reached the desired state
        var storedRequest = store.GetRequestById(reportId)!;
        Assert.Equal(targetState, storedRequest.Status);

        // Act — call the GET status endpoint directly
        var actionResult = controller.GetStatus(reportId);

        // Assert — must be 200 OK
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);

        // The value must be a TranscriptStatusResponseDto
        var dto = Assert.IsType<TranscriptStatusResponseDto>(okResult.Value);

        // Core property: response Status string must equal the enum name
        Assert.Equal(targetState.ToString(), dto.Status);

        // Sanity: ReportId and StudentId are preserved
        Assert.Equal(reportId, dto.ReportId);
        Assert.Equal("S-001", dto.StudentId);
    }
}
