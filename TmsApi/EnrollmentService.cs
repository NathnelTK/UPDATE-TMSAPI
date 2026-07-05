// --- Session 2 - Exercise 4: Structured Logging with Custom Levels ---
// We audit and apply structured logging guidelines to LegacyEnrollmentService.
// Guidelines:
// - Use structured logging placeholders {Placeholder} instead of string concatenation.
// - Use LogInformation for successful business operations (Enroll, Delete success).
// - Use LogWarning for unexpected but recoverable events (Duplicate enrollment, record not found).
//
// NOTE: This is the M4 in-memory proof-of-life service.
// M6 replaces this with a database-backed EnrollmentService in TmsApi.Services namespace.
// The "Legacy" prefix avoids naming conflicts with the new one.

namespace TmsApi.Legacy;

public interface ILegacyEnrollmentService
{
    Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode);
    Task<EnrollmentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}

public class LegacyEnrollmentService : ILegacyEnrollmentService
{
    private readonly Dictionary<string, EnrollmentRecord> _store = new();
    private readonly ILogger<LegacyEnrollmentService> _logger;

    public LegacyEnrollmentService(ILogger<LegacyEnrollmentService> logger)
    {
        _logger = logger;
    }

    public Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode)
    {
        // Check for duplicate enrollment
        var existing = _store.Values
            .FirstOrDefault(e => e.StudentId == studentId && e.CourseCode == courseCode);

        if (existing is not null)
        {
            // LogWarning: Unexpected but recoverable duplicate attempt with queryable properties
            _logger.LogWarning(
                "Duplicate enrollment attempt {StudentId} already in {CourseCode} (record {EnrollmentId})",
                studentId, courseCode, existing.Id);
            return Task.FromResult(existing);
        }

        var id = Guid.NewGuid().ToString("N")[..8];
        var record = new EnrollmentRecord(id, studentId, courseCode, DateTime.UtcNow);
        _store[id] = record;

        // LogInformation: Successful business event with queryable properties
        _logger.LogInformation(
            "Enrolled {StudentId} in {CourseCode} record {EnrollmentId}",
            studentId, courseCode, id);

        return Task.FromResult(record);
    }

    public Task<EnrollmentRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var record);

        if (record is null)
        {
            // LogWarning: unexpected non-existent record requested with queryable properties
            _logger.LogWarning("Enrollment {EnrollmentId} not found", id);
        }

        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync()
    {
        IReadOnlyList<EnrollmentRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);

        if (removed)
        {
            // LogInformation: Successful business event
            _logger.LogInformation("Deleted enrollment {EnrollmentId}", id);
        }
        else
        {
            // LogWarning: Delete failed because the record was not found
            _logger.LogWarning("Delete failed enrollment {EnrollmentId} not found", id);
        }

        return Task.FromResult(removed);
    }
}

public record EnrollmentRecord(
    string Id,
    string StudentId,
    string CourseCode,
    DateTime EnrolledAt);