namespace Api.Models;

public class Student
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty; // school-visible ID, unique
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QrToken> QrTokens { get; set; } = new List<QrToken>();
    public ICollection<AttendanceRecord> Attendances { get; set; } = new List<AttendanceRecord>();
}
