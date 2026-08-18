using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Controllers;

/// <summary>
/// M11 Session 1 - Exercise 2: Identity-backed authentication.
/// Replaces the M10 demo cookie controller (which used hardcoded credentials to
/// exercise browser transport). This version manages real accounts through
/// UserManager&lt;TmsUser&gt;, enforcing the enterprise password policy and
/// brute-force lockout configured in Program.cs. Verified over HTTP/Scalar.
///
/// NOTE: routed at /api/auth (unversioned) per the M11 verification steps, and the
/// request/response contract is now email-based — so the M10 Angular cookie
/// handshake no longer matches. Re-wiring the Angular client to JWTs is a later
/// session (M11 S3, Exercise 6), out of scope for these lab PDFs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<TmsUser> userManager,
    RoleManager<IdentityRole> roleManager) : ControllerBase
{
    public record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string Role);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            // Prevent account enumeration by returning a generic response.
            return Ok(new { message = "Registration request received." });
        }

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        // Ensure the requested role exists before assigning it.
        if (!await roleManager.RoleExistsAsync(request.Role))
        {
            await roleManager.CreateAsync(new IdentityRole(request.Role));
        }

        await userManager.AddToRoleAsync(user, request.Role);
        return Ok(new { message = "Registration successful." });
    }

    public record LoginRequest(string Email, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { detail = "Invalid credentials." });
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return StatusCode(423, new { detail = "Account locked due to multiple failed login attempts. Try again in 15 minutes." });
        }

        var validPassword = await userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            await userManager.AccessFailedAsync(user);
            return Unauthorized(new { detail = "Invalid credentials." });
        }

        // Reset the failed-attempt counter on successful login.
        await userManager.ResetAccessFailedCountAsync(user);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName
        });
    }
}
