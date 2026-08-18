using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;

namespace TmsApi.Api.Controllers;

/// <summary>
/// M10 Session 2 - Exercise 2: The Identity Handshake (demo transport layer).
/// On successful login the API writes the token into an <b>HttpOnly</b> "tms_auth"
/// cookie — client-side JavaScript (including any injected XSS payload) is
/// physically incapable of reading it, and the browser re-attaches it
/// automatically on every same-site request.
///
/// Uses hardcoded demo credentials for transport testing only. Module 11 replaces
/// this with ASP.NET Core Identity, BCrypt password hashing, and signed JWTs.
/// Routed as /api/v2/auth/* to match the V2 spine the Angular client targets.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("2.0")]
public class AuthController : ControllerBase
{
    // Demo account — M10 exercises transport (cookies/XSRF), not real accounts.
    private const string DemoUsername = "admin";
    private const string DemoPassword = "Password123!";

    [HttpPost("login")]
    public IActionResult Login(
        [FromBody] LoginRequest request,
        [FromServices] IWebHostEnvironment env)
    {
        // Validate credentials (demo account for M10 transport testing).
        if (request.Username == DemoUsername && request.Password == DemoPassword)
        {
            var dummyJwt = "header.payload.signature-demo-token";

            // Append the HttpOnly authentication cookie — JavaScript CANNOT read this.
            Response.Cookies.Append("tms_auth", dummyJwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = !env.IsDevelopment(), // HTTPS in prod; HTTP permitted locally in dev
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            return Ok(new UserProfileDto("System Admin", "Admin"));
        }

        return Unauthorized(new { detail = "Invalid username or password." });
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        // Inspect the cookie the browser attaches automatically on each request.
        if (Request.Cookies.TryGetValue("tms_auth", out _))
        {
            return Ok(new UserProfileDto("System Admin", "Admin"));
        }

        return Unauthorized(new { detail = "Session expired or missing authentication cookie." });
    }
}
