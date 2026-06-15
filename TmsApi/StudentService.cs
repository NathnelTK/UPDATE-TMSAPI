using Microsoft.Extensions.Logging;

namespace TmsApi;

public interface IStudentService
{
    Task<StudentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<StudentRecord>> GetAllAsync();
}

public class StudentService : IStudentService
{
    private readonly Dictionary<string, StudentRecord> _store = new();

    public StudentService()
    {
        // Seed some demo students
        var s1 = new StudentRecord("S-001", "Alice Smith");
        var s2 = new StudentRecord("S-002", "Bob Jones");
        _store[s1.Id] = s1;
        _store[s2.Id] = s2;
    }

    public Task<StudentRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var student);
        return Task.FromResult(student);
    }

    public Task<IReadOnlyList<StudentRecord>> GetAllAsync()
    {
        IReadOnlyList<StudentRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }
}

public record StudentRecord(string Id, string Name);
