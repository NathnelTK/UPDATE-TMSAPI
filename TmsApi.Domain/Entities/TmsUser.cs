using Microsoft.AspNetCore.Identity;

namespace TmsApi.Domain.Entities;

/// <summary>
/// M11 Session 1 - Exercise 2: application user extending ASP.NET Core Identity.
/// IdentityUser already supplies Id, UserName, Email, PasswordHash, lockout
/// counters, etc. We add the TMS-specific profile fields on top.
/// </summary>
public class TmsUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Department { get; set; }
}
