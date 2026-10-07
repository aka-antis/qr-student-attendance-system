namespace Api.Models;

public class Meeting
{
    public int Id { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? CreatedByTeacherId { get; set; }
    public Teacher? CreatedByTeacher { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AttendanceRecord> Attendances { get; set; } = new List<AttendanceRecord>();
}
