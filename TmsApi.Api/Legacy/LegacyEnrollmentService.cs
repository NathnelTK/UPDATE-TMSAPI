using Microsoft.Extensions.Logging;

namespace TmsApi.Api.Legacy;

public class LegacyEnrollmentService(ILogger<LegacyEnrollmentService> logger) : ILegacyEnrollmentService
{
    public Task<List<string>> GetAllAsync()
    {
        logger.LogInformation("Legacy GetAllAsync called");
        return Task.FromResult(new List<string> { "legacy-enrollment-1", "legacy-enrollment-2" });
    }

    public Task ProcessBatchAsync()
    {
        logger.LogInformation("Legacy batch processed");
        return Task.CompletedTask;
    }
}
