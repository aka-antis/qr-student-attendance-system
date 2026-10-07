namespace Api.Models;

/// <summary>
/// Stores token -&gt; student mapping. QR contains ONLY Token string.
/// No personal data is encoded in the QR itself.
/// </summary>
public class QrToken
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public string Token { get; set; } = string.Empty; // cryptographically random, unique
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
}
