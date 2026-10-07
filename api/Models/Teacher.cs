namespace Api.Models;

public class Teacher
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Teacher"; // Teacher | Admin
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Meeting> CreatedMeetings { get; set; } = new List<Meeting>();
    public ICollection<AttendanceRecord> Scans { get; set; } = new List<AttendanceRecord>();
}
