namespace TmsApi.Application.DTOs;

/// <summary>
/// M10 Session 2 - Exercise 2: the safe, client-facing view of a session.
/// Returned by /login and /me — deliberately carries NO token, only display data.
/// </summary>
public record UserProfileDto(string DisplayName, string Role);
