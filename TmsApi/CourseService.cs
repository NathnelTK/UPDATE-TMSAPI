namespace TmsApi;

public interface ICourseService
{
    Task<CourseRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<CourseRecord>> GetAllAsync();
}

public class CourseService : ICourseService
{
    private readonly Dictionary<string, CourseRecord> _store = new();

    public CourseService()
    {
        // Seed some demo courses
        var c1 = new CourseRecord("C-101", "Introduction to Programming");
        var c2 = new CourseRecord("C-201", "Data Structures");
        _store[c1.Id] = c1;
        _store[c2.Id] = c2;
    }

    public Task<CourseRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var course);
        return Task.FromResult(course);
    }

    public Task<IReadOnlyList<CourseRecord>> GetAllAsync()
    {
        IReadOnlyList<CourseRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }
}

public record CourseRecord(string Id, string Title);
