using Api.Data;
using Api.Dtos;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/students")]
[Authorize]
public class StudentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search)
    {
        var q = db.Students.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(s => s.FullName.Contains(search) || s.StudentCode.Contains(search));
        var students = await q.OrderBy(s => s.FullName)
            .Select(s => new
            {
                s.Id, s.FullName, s.StudentCode, s.Phone, s.IsActive, s.CreatedAt,
                ActiveToken = s.QrTokens.Where(t => t.IsActive).Select(t => t.Token).FirstOrDefault()
            }).ToListAsync();
        return Ok(students);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var s = await db.Students.Include(x => x.QrTokens).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        return Ok(new
        {
            s.Id, s.FullName, s.StudentCode, s.Phone, s.IsActive,
            ActiveToken = s.QrTokens.FirstOrDefault(t => t.IsActive)?.Token,
            TokensCount = s.QrTokens.Count
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStudentRequest req)
    {
        if (await db.Students.AnyAsync(s => s.StudentCode == req.StudentCode))
            return Conflict(new { message = "StudentCode already exists." });
        var s = new Student
        {
            FullName = req.FullName.Trim(),
            StudentCode = req.StudentCode.Trim(),
            Phone = req.Phone?.Trim()
        };
        db.Students.Add(s);
        await db.SaveChangesAsync();

        // Issue first token immediately.
        var token = await IssueNewTokenAsync(s.Id);
        return CreatedAtAction(nameof(Get), new { id = s.Id },
            new { s.Id, s.FullName, s.StudentCode, ActiveToken = token });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateStudentRequest req)
    {
        var s = await db.Students.FindAsync(id);
        if (s is null) return NotFound();
        s.FullName = req.FullName.Trim();
        s.Phone = req.Phone?.Trim();
        s.IsActive = req.IsActive;
        await db.SaveChangesAsync();
        return Ok(new { message = "Updated." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await db.Students.FindAsync(id);
        if (s is null) return NotFound();
        db.Students.Remove(s);
        await db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }

    // Returns student info + QR payload (token only inside QR) + base64 PNG.
    [HttpGet("{id:int}/qr")]
    public async Task<IActionResult> GetQr(int id)
    {
        var s = await db.Students.Include(x => x.QrTokens).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound(new { message = "Student not found." });
        var active = s.QrTokens.FirstOrDefault(t => t.IsActive);
        if (active is null) return NotFound(new { message = "No active QR token. Regenerate one." });
        return Ok(new
        {
            studentId = s.Id,
            studentName = s.FullName,
            studentCode = s.StudentCode,
            token = active.Token, // QR encodes ONLY this
            qrBase64 = QrCodeService.GenerateBase64Png(active.Token),
            note = "QR contains only the random token. Name/code shown outside QR."
        });
    }

    [HttpGet("{id:int}/qr.png")]
    [AllowAnonymous] // printable card image; token itself is unguessable
    public async Task<IActionResult> GetQrPng(int id)
    {
        var active = await db.QrTokens
            .Where(t => t.StudentId == id && t.IsActive)
            .Select(t => t.Token).FirstOrDefaultAsync();
        if (active is null) return NotFound();
        return File(QrCodeService.GeneratePng(active), "image/png", $"student-{id}-qr.png");
    }

    [HttpPost("{id:int}/regenerate")]
    public async Task<IActionResult> Regenerate(int id)
    {
        if (!await db.Students.AnyAsync(s => s.Id == id)) return NotFound();
        var token = await IssueNewTokenAsync(id);
        return Ok(new { token, qrBase64 = QrCodeService.GenerateBase64Png(token) });
    }

    [HttpPost("{id:int}/revoke")]
    public async Task<IActionResult> Revoke(int id)
    {
        var tokens = await db.QrTokens.Where(t => t.StudentId == id && t.IsActive).ToListAsync();
        foreach (var t in tokens) { t.IsActive = false; t.RevokedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync();
        return Ok(new { message = "All active tokens revoked." });
    }

    private async Task<string> IssueNewTokenAsync(int studentId)
    {
        var old = await db.QrTokens.Where(t => t.StudentId == studentId && t.IsActive).ToListAsync();
        foreach (var t in old) { t.IsActive = false; t.RevokedAt = DateTime.UtcNow; }

        // Ensure uniqueness (collision chance negligible, but retry anyway).
        for (var i = 0; i < 5; i++)
        {
            var token = SecureTokenService.GenerateToken();
            if (await db.QrTokens.AnyAsync(t => t.Token == token)) continue;
            db.QrTokens.Add(new QrToken { StudentId = studentId, Token = token, IsActive = true });
            await db.SaveChangesAsync();
            return token;
        }
        throw new InvalidOperationException("Could not generate unique token.");
    }
}
