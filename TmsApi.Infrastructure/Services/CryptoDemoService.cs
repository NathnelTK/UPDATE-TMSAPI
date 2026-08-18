namespace TmsApi.Infrastructure.Services;

/// <summary>
/// M11 Session 1 - Exercise 1: demonstrates BCrypt salting + work factor.
/// BCrypt generates a unique random salt on every call and prepends it to the
/// hash, so hashing the same password twice yields two different strings — yet
/// both verify against the plaintext. workFactor 12 => 2^12 key-expansion
/// iterations, deliberately slowing brute-force attacks.
///
/// Educational only. Production code never hand-rolls hashing — it uses the
/// UserManager provided by ASP.NET Core Identity (see Exercise 2).
/// </summary>
public class CryptoDemoService
{
    public string HashUserPassword(string plainText)
    {
        // BCrypt automatically generates a unique salt and prepends it to the hash.
        // workFactor: 12 means 2^12 key-expansion iterations.
        return BCrypt.Net.BCrypt.HashPassword(plainText, workFactor: 12);
    }

    public bool VerifyUserPassword(string plainText, string hashedDbPassword)
    {
        return BCrypt.Net.BCrypt.Verify(plainText, hashedDbPassword);
    }
}
