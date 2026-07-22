namespace TmsApi.Infrastructure.Caching;

/// <summary>
/// Centralized cache key definitions with schema versioning.
/// Bump SchemaVersion when DTO shapes change to prevent stale data.
/// </summary>
public static class CacheKeys
{
    private const string SchemaVersion = "v2";

    public static string Course(string code) => $"{SchemaVersion}:course:{code}";
    public static string CoursesAll => $"{SchemaVersion}:courses:all";
    public static string CourseById(int id) => $"{SchemaVersion}:course:id:{id}";
    public static string CoursesPopular => $"{SchemaVersion}:courses:popular";
    public static string SearchCourses(string term) => $"{SchemaVersion}:courses:search:{term}";

    public const string CoursesTag = "courses";
}