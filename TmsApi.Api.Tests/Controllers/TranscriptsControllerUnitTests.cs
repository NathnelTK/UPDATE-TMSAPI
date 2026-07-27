using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using TmsApi.Api.Controllers;
using TmsApi.Api.Transcripts;

namespace TmsApi.Api.Tests.Controllers;

/// <summary>
/// Unit tests for TranscriptsController (task 4.3).
/// Validates: Requirements 1.1–1.5, 2.1–2.4, 5.1–5.6
/// </summary>
public class TranscriptsControllerUnitTests
{
    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    private static StatusStore CreateStore() =>
        new StatusStore(NullLogger<StatusStore>.Instance);

    private static TranscriptsController CreateController(StatusStore store) =>
        new TranscriptsController(store, NullLogger<TranscriptsController>.Instance);

    private static TranscriptRequestDto DefaultRequest(string studentId = "S-001") =>
        new TranscriptRequestDto(studentId);

    // -------------------------------------------------------------------
    // POST tests
    // -------------------------------------------------------------------

    /// <summary>
    /// POST without Idempotency-Key header returns 400 Bad Request.
    /// Validates: Requirements 1.1 (must have idempotency key)
    /// </summary>
    [Fact]
    public void Post_WithoutIdempotencyKey_Returns400()
    {
        // Arrange
        var store = CreateStore();
        var controller = CreateController(store);

        // Act — pass null idempotencyKey
        var result = controller.RequestTranscript(DefaultRequest(), idempotencyKey: null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// POST with empty string Idempotency-Key header returns 400 Bad Request.
    /// Validates: Requirements 1.1
    /// </summary>
    [Fact]
    public void Post_WithEmptyIdempotencyKey_Returns400()
    {
        // Arrange
        var store = CreateStore();
        var controller = CreateController(store);

        // Act — pass empty string idempotencyKey
        var result = controller.RequestTranscript(DefaultRequest(), idempotencyKey: string.Empty);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// POST with a duplicate Idempotency-Key returns 202 Accepted with the same
    /// reportId and duplicate = true in the body.
    /// Validates: Requirements 2.1, 2.2, 2.3
    /// </summary>
    [Fact]
    public void Post_WithDuplicateIdempotencyKey_Returns202WithSameReportId()
    {
        // Arrange
        var store = CreateStore();
        var controller = CreateController(store);
        var key = Guid.NewGuid().ToString();

        // First request — creates a new entry
        var firstResult = controller.RequestTranscript(DefaultRequest(), idempotencyKey: key);
        var firstAccepted = Assert.IsType<AcceptedResult>(firstResult);
        var firstBody = firstAccepted.Value!;
        var firstReportId = (string)firstBody.GetType().GetProperty("reportId")!.GetValue(firstBody)!;

        // Act — second request with the same key
        var secondResult = controller.RequestTranscript(DefaultRequest(), idempotencyKey: key);

        // Assert
        var secondAccepted = Assert.IsType<AcceptedResult>(secondResult);
        Assert.Equal(202, secondAccepted.StatusCode);

        var secondBody = secondAccepted.Value!;
        var secondReportId = (string)secondBody.GetType().GetProperty("reportId")!.GetValue(secondBody)!;
        var duplicate = (bool)secondBody.GetType().GetProperty("duplicate")!.GetValue(secondBody)!;

        Assert.Equal(firstReportId, secondReportId);
        Assert.True(duplicate);
    }

    // -------------------------------------------------------------------
    // GET tests
    // -------------------------------------------------------------------

    /// <summary>
    /// GET for an unknown reportId returns 404 Not Found.
    /// Validates: Requirements 5.5
    /// </summary>
    [Fact]
    public void Get_UnknownReportId_Returns404()
    {
        // Arrange
        var store = CreateStore();
        var controller = CreateController(store);

        // Act
        var result = controller.GetStatus("nonexistent-id");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// GET for a request in Ready state includes the downloadUrl.
    /// Validates: Requirements 5.1, 5.3
    /// </summary>
    [Fact]
    public void Get_ReadyRequest_IncludesDownloadUrl()
    {
        // Arrange
        var store = CreateStore();
        var controller = CreateController(store);
        var key = Guid.NewGuid().ToString();
        const string expectedDownloadUrl = "https://cdn.tms.local/transcripts/test.pdf";

        var reportId = store.EnqueueRequest("S-001", key);

        // Transition through the valid path to Ready
        store.TryTransitionState(reportId, TranscriptStatus.Processing);
        store.TryTransitionState(reportId, TranscriptStatus.Ready, downloadUrl: expectedDownloadUrl);

        // Act
        var result = controller.GetStatus(reportId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<TranscriptStatusResponseDto>(okResult.Value);

        Assert.NotNull(dto.DownloadUrl);
        Assert.Equal(expectedDownloadUrl, dto.DownloadUrl);
        Assert.Equal("Ready", dto.Status);
    }

    /// <summary>
    /// GET for a request in Failed state includes the errorMessage.
    /// Validates: Requirements 5.1, 5.4
    /// </summary>
    [Fact]
    public void Get_FailedRequest_IncludesErrorMessage()
    {
        // Arrange
        var store = CreateStore();
        var controller = CreateController(store);
        var key = Guid.NewGuid().ToString();
        const string expectedErrorMessage = "PDF generation service unavailable";

        var reportId = store.EnqueueRequest("S-002", key);

        // Transition through the valid path to Failed
        store.TryTransitionState(reportId, TranscriptStatus.Processing);
        store.TryTransitionState(reportId, TranscriptStatus.Failed, errorMessage: expectedErrorMessage);

        // Act
        var result = controller.GetStatus(reportId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<TranscriptStatusResponseDto>(okResult.Value);

        Assert.NotNull(dto.ErrorMessage);
        Assert.Equal(expectedErrorMessage, dto.ErrorMessage);
        Assert.Equal("Failed", dto.Status);
    }

    /// <summary>
    /// GET for a Queued request returns QueuedAt as non-default and ProcessingAt as null.
    /// GET for a Processing request returns both QueuedAt and ProcessingAt as non-null.
    /// Validates: Requirements 5.1, 5.2, 15.1, 15.2
    /// </summary>
    [Fact]
    public void Get_QueuedRequest_HasQueuedAtAndNullProcessingAt()
    {
        // Arrange
        var store = CreateStore();
        var controller = CreateController(store);
        var key = Guid.NewGuid().ToString();

        var reportId = store.EnqueueRequest("S-003", key);

        // Act — still in Queued state
        var result = controller.GetStatus(reportId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<TranscriptStatusResponseDto>(okResult.Value);

        Assert.Equal("Queued", dto.Status);
        Assert.NotEqual(default, dto.QueuedAt);   // QueuedAt must be set
        Assert.Null(dto.ProcessingAt);             // ProcessingAt must be null
    }

    /// <summary>
    /// GET for a Processing request returns non-null QueuedAt and non-null ProcessingAt.
    /// Validates: Requirements 5.2, 15.1, 15.2
    /// </summary>
    [Fact]
    public void Get_ProcessingRequest_HasQueuedAtAndProcessingAt()
    {
        // Arrange
        var store = CreateStore();
        var controller = CreateController(store);
        var key = Guid.NewGuid().ToString();

        var reportId = store.EnqueueRequest("S-004", key);

        // Transition to Processing
        store.TryTransitionState(reportId, TranscriptStatus.Processing);

        // Act
        var result = controller.GetStatus(reportId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<TranscriptStatusResponseDto>(okResult.Value);

        Assert.Equal("Processing", dto.Status);
        Assert.NotEqual(default, dto.QueuedAt);   // QueuedAt must be set
        Assert.NotNull(dto.ProcessingAt);          // ProcessingAt must now be set
    }
}
