using Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("meeting/{meetingId:int}")]
    public async Task<IActionResult> MeetingReport(int meetingId)
    {
        var meeting = await db.Meetings.FindAsync(meetingId);
        if (meeting is null) return NotFound(new { message = "Meeting not found." });
        var rows = await db.AttendanceRecords.Include(a => a.Student)
            .Where(a => a.MeetingId == meetingId)
            .OrderBy(a => a.Student!.FullName)
            .Select(a => new { a.StudentId, StudentName = a.Student!.FullName, a.Student!.StudentCode, a.ScannedAt })
            .ToListAsync();
        return Ok(new { meeting.Id, meeting.CourseName, meeting.Date, count = rows.Count, rows });
    }

    [HttpGet("student/{studentId:int}")]
    public async Task<IActionResult> StudentReport(int studentId)
    {
        var student = await db.Students.FindAsync(studentId);
        if (student is null) return NotFound(new { message = "Student not found." });
        var totalMeetings = await db.Meetings.CountAsync();
        var attended = await db.AttendanceRecords
            .Include(a => a.Meeting)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.ScannedAt)
            .Select(a => new { a.MeetingId, CourseName = a.Meeting!.CourseName, MeetingDate = a.Meeting!.Date, a.ScannedAt })
            .ToListAsync();
        double pct = totalMeetings == 0 ? 0 : Math.Round(attended.Count * 100.0 / totalMeetings, 2);
        return Ok(new
        {
            student.Id, student.FullName, student.StudentCode,
            totalMeetings, attendedCount = attended.Count,
            percentage = pct, history = attended
        });
    }

    [HttpGet("meeting/{meetingId:int}/export.csv")]
    public async Task<IActionResult> ExportMeetingCsv(int meetingId)
    {
        var meeting = await db.Meetings.FindAsync(meetingId);
        if (meeting is null) return NotFound();
        var rows = await db.AttendanceRecords.Include(a => a.Student)
            .Where(a => a.MeetingId == meetingId)
            .OrderBy(a => a.Student!.FullName).ToListAsync();
        var sb = new StringBuilder("StudentId,StudentName,StudentCode,Course,Date,ScannedAtUtc\n");
        foreach (var a in rows)
            sb.AppendLine($"{a.StudentId},\"{a.Student!.FullName}\",{a.Student.StudentCode},\"{meeting.CourseName}\",{meeting.Date:yyyy-MM-dd},{a.ScannedAt:O}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"meeting-{meetingId}-attendance.csv");
    }

    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportAllCsv([FromQuery] int? meetingId, [FromQuery] int? studentId)
    {
        var q = db.AttendanceRecords.Include(a => a.Student).Include(a => a.Meeting).AsQueryable();
        if (meetingId.HasValue) q = q.Where(a => a.MeetingId == meetingId.Value);
        if (studentId.HasValue) q = q.Where(a => a.StudentId == studentId.Value);
        var rows = await q.OrderByDescending(a => a.ScannedAt).ToListAsync();
        var sb = new StringBuilder("StudentId,StudentName,StudentCode,MeetingId,Course,Date,ScannedAtUtc\n");
        foreach (var a in rows)
            sb.AppendLine($"{a.StudentId},\"{a.Student!.FullName}\",{a.Student.StudentCode},{a.MeetingId},\"{a.Meeting!.CourseName}\",{a.Meeting.Date:yyyy-MM-dd},{a.ScannedAt:O}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "attendance-export.csv");
    }
}
