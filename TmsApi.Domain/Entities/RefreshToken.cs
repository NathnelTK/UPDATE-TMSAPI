namespace TmsApi.Domain.Entities;

/// <summary>
/// M11 Session 2 - Exercise 5: persisted refresh token backing JWT rotation.
/// Each successful login issues a short-lived access token (JWT) plus a long-lived
/// refresh token stored here. Refreshing rotates the token — the presented token is
/// marked IsUsed and a fresh one is issued. Presenting an already-used token is
/// treated as theft: every token for that user is revoked.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
}
