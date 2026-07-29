using Microsoft.Extensions.Logging; // M7 Session 4 - Exercise 8: ILogger<> for CertificateService
using System.Net.Http.Json; // M7 Session 4 - Exercise 8: PostAsJsonAsync/ReadFromJsonAsync extensions
using Polly;
using Polly.Registry;
using Polly.Timeout;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.ExternalServices;

// --- M7 Session 4 - Exercise 8: Certificate service with Polly v8 resilience ---
// Composes a named resilience pipeline ("certificate-api") that handles three
// realistic failure modes of the external certificate-printing service:
//   1. Transient 503 → retried with jittered exponential backoff
//   2. Hung downstream → cut by per-request 5s timeout (TimeoutRejectedException)
//   3. Sustained outage → circuit breaker fails fast, then half-open probes recovery
// Non-transient 4xx responses are NOT retried — they surface as InvalidOperationException.
public class CertificateService(
    ResiliencePipelineProvider<string> pipelineProvider,
    HttpClient httpClient,
    ILogger<CertificateService> logger)
    : ICertificateService
{
    public async Task<CertificateResult> IssueCertificateAsync(
        int studentId, string courseCode, CancellationToken ct)
    {
        // Fetch the named pipeline registered in Program.cs (Timeout → CircuitBreaker → Retry).
        var pipeline = pipelineProvider.GetPipeline("certificate-api");

        return await pipeline.ExecuteAsync(async token =>
        {
            logger.LogInformation(
                "Requesting certificate for student {StudentId}, course {CourseCode}",
                studentId, courseCode);

            // POST to the fake certificate endpoint (lab fixture in the same process).
            using var response = await httpClient.PostAsJsonAsync(
                "/fake/certificates",
                new { StudentId = studentId, CourseCode = courseCode },
                token);

            // 5xx → throw HttpRequestException so Polly retries and the breaker counts it.
            if ((int)response.StatusCode >= 500)
                throw new HttpRequestException($"Upstream {(int)response.StatusCode}");

            // 4xx → do NOT throw HttpRequestException; Polly must NOT retry non-transient faults.
            // Instead, throw InvalidOperationException which is outside ShouldHandle, so no retry.
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(token);
                throw new InvalidOperationException(
                    $"Certificate service rejected: {(int)response.StatusCode} {err}");
            }

            // Success → deserialize the result.
            return await response.Content.ReadFromJsonAsync<CertificateResult>(
                cancellationToken: token)
                ?? throw new InvalidOperationException("Empty certificate response.");
        }, ct);
    }
}