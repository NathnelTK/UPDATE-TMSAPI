using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TmsApi.Api.Hubs;
using TmsApi.Api.Legacy;

namespace TmsApi.Api.Transcripts;

public class TranscriptGeneratorOptions
{
    public int ProcessingDelayMs { get; set; } = 5000; // default 5 seconds
}

public class TranscriptGenerator : BackgroundService
{
    private readonly StatusStore _statusStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<TmsHub, ITmsHubClient> _hubContext;
    private readonly ILogger<TranscriptGenerator> _logger;
    private readonly TranscriptGeneratorOptions _options;

    public TranscriptGenerator(
        StatusStore statusStore,
        IServiceScopeFactory scopeFactory,
        IHubContext<TmsHub, ITmsHubClient> hubContext,
        ILogger<TranscriptGenerator> logger,
        TranscriptGeneratorOptions options)
    {
        _statusStore = statusStore;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var request = _statusStore.GetNextQueuedRequest();
            if (request is not null)
                await ProcessRequestAsync(request, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task ProcessRequestAsync(TranscriptRequest request, CancellationToken ct)
    {
        try
        {
            // Req 16.2: log ReportId + Processing transition
            _statusStore.TryTransitionState(request.ReportId, TranscriptStatus.Processing);
            _logger.LogInformation(
                "Transcript {ReportId} transitioned to Processing",
                request.ReportId);

            // Req 14.2–14.3: create and dispose a service scope for scoped dependencies
            using var scope = _scopeFactory.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<ILegacyEnrollmentService>();

            // Req 3.3: simulate PDF generation with configurable delay
            await Task.Delay(_options.ProcessingDelayMs, ct);

            var downloadUrl = $"https://cdn.tms.local/transcripts/{request.ReportId}.pdf";

            // Req 16.3: log ReportId + completion
            _statusStore.TryTransitionState(request.ReportId, TranscriptStatus.Ready, downloadUrl);
            _logger.LogInformation(
                "Transcript {ReportId} completed successfully. DownloadUrl: {DownloadUrl}",
                request.ReportId, downloadUrl);

            // Req 8.1, 16.7: send SignalR notification to the student's group
            var groupName = $"student:{request.StudentId}";
            await _hubContext.Clients
                .Group(groupName)
                .TranscriptReady(request.ReportId, request.StudentId, downloadUrl);

            _logger.LogInformation(
                "TranscriptReady notification sent for ReportId={ReportId}, StudentId={StudentId}, Group={Group}",
                request.ReportId, request.StudentId, groupName);
        }
        catch (OperationCanceledException)
        {
            // Rethrow cancellation — do not treat it as a processing failure
            throw;
        }
        catch (Exception ex)
        {
            // Req 16.4: log error with ReportId and stack trace
            _logger.LogError(ex, "Transcript generation failed for {ReportId}", request.ReportId);
            _statusStore.TryTransitionState(
                request.ReportId,
                TranscriptStatus.Failed,
                errorMessage: ex.Message);
        }
    }
}
