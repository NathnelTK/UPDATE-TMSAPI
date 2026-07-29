namespace TmsApi.Application.Interfaces;

// --- M7 Session 4 - Exercise 8: Certificate service contract ---
// The TMS calls an external certificate-printing service. This interface decouples
// the API layer from the resilience strategy (Polly v8) used by the implementation.
public sealed record CertificateResult(string Status, int Attempt);

public interface ICertificateService
{
    /// <summary>
    /// Issue a certificate for a student who completed a course.
    /// The implementation wraps the HTTP call in a Polly v8 resilience pipeline
    /// (Timeout → CircuitBreaker → Retry) scoped to transient failures only.
    /// </summary>
    Task<CertificateResult> IssueCertificateAsync(
        int studentId, string courseCode, CancellationToken ct);
}