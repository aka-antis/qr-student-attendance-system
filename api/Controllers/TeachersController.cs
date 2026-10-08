using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/teachers")]
[Authorize(Roles = "Admin")]
public class TeachersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.Teachers
        .OrderBy(t => t.Username)
        .Select(t => new { t.Id, t.Username, t.FullName, t.Role, t.IsActive, t.CreatedAt })
        .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CreateTeacherRequest req)
    {
        if (await db.Teachers.AnyAsync(t => t.Username == req.Username))
            return Conflict(new { message = "Username already exists." });
        var role = req.Role == "Admin" ? "Admin" : "Teacher";
        var t = new Teacher
        {
            Username = req.Username.Trim(),
            FullName = req.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = role,
        };
        db.Teachers.Add(t);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { id = t.Id },
            new { t.Id, t.Username, t.FullName, t.Role });
    }

    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var t = await db.Teachers.FindAsync(id);
        if (t is null) return NotFound();
        t.IsActive = false;
        await db.SaveChangesAsync();
        return Ok(new { message = "Deactivated." });
    }
}
