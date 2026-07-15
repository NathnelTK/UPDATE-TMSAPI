namespace TmsApi.Dtos;

/// <summary>
/// HATEOAS link DTO — tells the client what actions are available
/// on a resource without the client needing to construct URLs.
/// Href: the resolved URL (never null in production).
/// Rel: the relationship (self, update, delete, enrollments, enroll).
/// Method: the HTTP method to use (GET, PUT, DELETE, POST).
/// </summary>
public record LinkDto(string Href, string Rel, string Method);