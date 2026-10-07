namespace Api.Models;

public class AttendanceRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int MeetingId { get; set; }
    public Meeting? Meeting { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow; // server time
    public int? ScannedByTeacherId { get; set; }
    public Teacher? ScannedByTeacher { get; set; }
}
