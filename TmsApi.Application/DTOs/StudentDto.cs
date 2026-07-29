using System.Text.Json.Serialization;

namespace TmsApi.Application.DTOs;

// --- M7 Session 4 - Exercise 7: Student DTO with belt-and-braces sensitive field protection ---
// [JsonIgnore] is the second line of defence. If a future controller forgets to call
// ShapeData and serialises this DTO directly, [JsonIgnore] still hides InternalNotes.
// Whitelist (in DataShaper) + [JsonIgnore] is the production pattern.
public record StudentDto(int Id, string FullName, string Email)
{
    // Sensitive field that must never escape to the API response, even if shaping is bypassed.
    [JsonIgnore] public string? InternalNotes { get; init; }
}

// Whitelist for StudentDto — only these fields may be requested via ?fields=.
public static class StudentDtoFields
{
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(StudentDto.Id),
        nameof(StudentDto.FullName),
        nameof(StudentDto.Email)
        // Note: InternalNotes is intentionally NOT in the whitelist.
    };
}