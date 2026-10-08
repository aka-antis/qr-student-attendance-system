using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController(AppDbContext db, IConfiguration cfg) : ControllerBase
{
    /// <summary>
    /// Core scan endpoint. Client sends ONLY meetingId + raw QR token.
    /// Student is resolved server-side from the token. Never trust client-sent student id.
    /// </summary>
    [HttpPost("scan")]
    public async Task<ActionResult<ScanResponse>> Scan(ScanRequest req)
    {
        var token = req.Token.Trim();
        if (token.Length == 0)
            return BadRequest(new ScanResponse("error", "Empty QR token.", null, null, req.MeetingId, null));

        var meeting = await db.Meetings.FindAsync(req.MeetingId);
        if (meeting is null)
            return NotFound(new ScanResponse("error", "Meeting not found.", null, null, req.MeetingId, null));

        if (IsOutsideMeetingWindow(meeting, out var windowMessage))
            return BadRequest(new ScanResponse("error", windowMessage, null, null, req.MeetingId, null));

        // Resolve student from token ONLY if token is active.
        var qr = await db.QrTokens.Include(t => t.Student)
            .FirstOrDefaultAsync(t => t.Token == token && t.IsActive);
        if (qr?.Student is null || !qr.Student.IsActive)
            return BadRequest(new ScanResponse("error", "Invalid or revoked QR code.", null, null, req.MeetingId, null));

        // Duplicate check (friendly message); DB unique index is the real guard.
        var existing = await db.AttendanceRecords.FirstOrDefaultAsync(a =>
            a.StudentId == qr.StudentId && a.MeetingId == req.MeetingId);
        if (existing is not null)
            return Conflict(new ScanResponse("duplicate",
                $"Already registered: {qr.Student.FullName}.",
                qr.Student.FullName, qr.StudentId, req.MeetingId, existing.ScannedAt));

        var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var record = new AttendanceRecord
        {
            StudentId = qr.StudentId,
            MeetingId = req.MeetingId,
            ScannedAt = DateTime.UtcNow, // server time
            ScannedByTeacherId = teacherId
        };
        db.AttendanceRecords.Add(record);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Race between two scans: unique constraint fired.
            return Conflict(new ScanResponse("duplicate",
                $"Already registered: {qr.Student.FullName}.",
                qr.Student.FullName, qr.StudentId, req.MeetingId, null));
        }

        return Ok(new ScanResponse("success",
            $"Attendance recorded: {qr.Student.FullName}.",
            qr.Student.FullName, qr.StudentId, req.MeetingId, record.ScannedAt));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> List([FromQuery] int? meetingId, [FromQuery] int? studentId, [FromQuery] string? search)
    {
        var q = db.AttendanceRecords
            .Include(a => a.Student).Include(a => a.Meeting).Include(a => a.ScannedByTeacher)
            .AsQueryable();
        if (meetingId.HasValue) q = q.Where(a => a.MeetingId == meetingId.Value);
        if (studentId.HasValue) q = q.Where(a => a.StudentId == studentId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(a => a.Student!.FullName.Contains(search) || a.Student!.StudentCode.Contains(search)
                || a.Meeting!.CourseName.Contains(search));
        var list = await q.OrderByDescending(a => a.ScannedAt)
            .Select(a => new
            {
                a.Id,
                a.StudentId,
                StudentName = a.Student!.FullName,
                StudentCode = a.Student!.StudentCode,
                a.MeetingId,
                CourseName = a.Meeting!.CourseName,
                MeetingDate = a.Meeting!.Date,
                a.ScannedAt,
                ScannedBy = a.ScannedByTeacher != null ? a.ScannedByTeacher.FullName : null
            }).ToListAsync();
        return Ok(list);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await db.AttendanceRecords.FindAsync(id);
        if (a is null) return NotFound(new { message = "Attendance record not found." });
        db.AttendanceRecords.Remove(a);
        await db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }

    /// <summary>
    /// Optional guard (off by default): reject scans outside the meeting's
    /// scheduled window. Enable with Attendance:EnforceMeetingWindow=true.
    /// Compared in server-local time since meetings are scheduled locally.
    /// </summary>
    private bool IsOutsideMeetingWindow(Meeting meeting, out string message)
    {
        message = string.Empty;
        if (!cfg.GetValue<bool>("Attendance:EnforceMeetingWindow", false))
            return false;
        var grace = cfg.GetValue<int>("Attendance:GraceMinutes", 15);
        var start = meeting.Date.ToDateTime(meeting.StartTime).AddMinutes(-grace);
        var end = meeting.Date.ToDateTime(meeting.EndTime).AddMinutes(grace);
        var now = DateTime.Now;
        if (now < start || now > end)
        {
            message = $"Scan rejected: outside the meeting window " +
                $"({meeting.Date:yyyy-MM-dd} {meeting.StartTime:HH\\:mm}-{meeting.EndTime:HH\\:mm}, +/-{grace} min).";
            return true;
        }
        return false;
    }
}
