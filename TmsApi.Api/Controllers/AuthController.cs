using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers;

/// <summary>
/// M11 Session 2 - Exercise 4 &amp; 5: JWT-backed authentication.
/// Builds on the Session 1 Identity controller: login now verifies the password
/// through UserManager&lt;TmsUser&gt; (enterprise policy + lockout) and, on success,
/// issues a short-lived JWT access token plus a long-lived, single-use refresh token.
/// Refreshing rotates the token; replaying a used token trips theft detection and
/// revokes every session for that user.
///
/// NOTE: routed at /api/auth (unversioned). Re-wiring the Angular client to consume
/// these tokens is beyond the scope of the M11 lab PDFs (S1/S2), so the SPA cookie
/// handshake from M10 no longer matches this contract.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<TmsUser> userManager,
    RoleManager<IdentityRole> roleManager,
    TmsDbContext db,
    TokenService tokenService) : ControllerBase
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

    // --- M11 Session 3 - Exercise 7 Step 1: throttle login attempts ---
    // 5 attempts/minute via the AuthLimiter fixed window; a 6th returns 429.
    // Defence-in-depth alongside Identity's per-account lockout (423 Locked).
    [EnableRateLimiting("AuthLimiter")]
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

        var tokens = await IssueTokensAsync(user);
        return Ok(tokens);
    }

    public record RefreshRequest(string RefreshToken);

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var existingToken = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (existingToken == null)
        {
            return Unauthorized(new { detail = "Invalid refresh token." });
        }

        // Theft detection: a token that was already rotated is being replayed.
        // Someone is holding a stolen copy — revoke every session for this user.
        // Checked BEFORE the revoked/expired guard so a reused token is always
        // reported as theft.
        if (existingToken.IsUsed)
        {
            var userTokens = await db.RefreshTokens
                .Where(t => t.UserId == existingToken.UserId)
                .ToListAsync();
            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
            }
            await db.SaveChangesAsync();

            return Unauthorized(new { detail = "Token theft detected. All user sessions revoked." });
        }

        if (existingToken.IsRevoked || existingToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { detail = "Refresh token expired or revoked." });
        }

        var user = await userManager.FindByIdAsync(existingToken.UserId);
        if (user == null)
        {
            return Unauthorized(new { detail = "Invalid refresh token." });
        }

        // Rotate: mark the presented token used, then issue a fresh pair.
        existingToken.IsUsed = true;
        var tokens = await IssueTokensAsync(user);
        return Ok(tokens);
    }

    /// <summary>
    /// Mints a JWT access token and persists a new single-use refresh token for the
    /// user. Shared by login and refresh so both return the identical shape.
    /// </summary>
    private async Task<object> IssueTokensAsync(TmsUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateJwt(user, roles);

        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        return new
        {
            accessToken,
            refreshToken = refreshToken.Token
        };
    }
}
