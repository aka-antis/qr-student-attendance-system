using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/meetings")]
[Authorize]
public class MeetingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.Meetings
        .OrderByDescending(m => m.Date).ThenByDescending(m => m.StartTime)
        .Select(m => new
        {
            m.Id, m.CourseName, m.Date, m.StartTime, m.EndTime, m.CreatedAt,
            AttendedCount = m.Attendances.Count
        }).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var m = await db.Meetings.FindAsync(id);
        return m is null ? NotFound(new { message = "Meeting not found." }) : Ok(m);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMeetingRequest req)
    {
        if (req.EndTime <= req.StartTime)
            return BadRequest(new { message = "EndTime must be after StartTime." });
        var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var m = new Meeting
        {
            CourseName = req.CourseName.Trim(),
            Date = req.Date,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            CreatedByTeacherId = teacherId
        };
        db.Meetings.Add(m);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = m.Id }, m);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await db.Meetings.FindAsync(id);
        if (m is null) return NotFound(new { message = "Meeting not found." });
        db.Meetings.Remove(m);
        await db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}
