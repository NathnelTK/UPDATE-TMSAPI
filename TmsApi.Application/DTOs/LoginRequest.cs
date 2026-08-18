namespace TmsApi.Application.DTOs;

/// <summary>
/// M10 Session 2 - Exercise 2: credentials posted to /api/v2/auth/login.
/// Demo transport DTO — the real Identity-backed login (with Email) arrives in M11.
/// </summary>
public record LoginRequest(string Username, string Password);
