// --- Session 2 - Exercise 2: Captive Dependency Resolution ---
// EnrollmentWorker is a Singleton service. It cannot directly depend on a Scoped service
// like IEnrollmentService because doing so would keep the scoped service alive (captive) for
// the lifetime of the application, causing potential memory leaks and staleness.
// To fix this captive dependency, we inject IServiceScopeFactory and resolve IEnrollmentService
// within a short-lived using-wrapped scope.

public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ProcessBatch()
    {
        // Create a short-lived scope
        using var scope = _scopeFactory.CreateScope();

        // Resolve the scoped IEnrollmentService from the scope's service provider
        var enrollmentService = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        // Safely perform operations using the scoped service
        // (For training/smoke-testing purposes, we can invoke a dummy operation or write a log)
        var allEnrollments = enrollmentService.GetAllAsync().GetAwaiter().GetResult();
    }
}
