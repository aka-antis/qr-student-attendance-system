using System.Security.Cryptography;

namespace Api.Services;

/// <summary>Generates cryptographically random QR tokens. QR holds ONLY this token.</summary>
public static class SecureTokenService
{
    public static string GenerateToken(int numBytes = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(numBytes);
        // Base64Url without padding: ~43 chars for 32 bytes. No personal data.
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
