using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public record LoginRequest(
    [Required, StringLength(64, MinimumLength = 1)] string Username,
    [Required, StringLength(128, MinimumLength = 1)] string Password);

public record LoginResponse(string Token, string Username, string FullName, string Role);

public record CreateTeacherRequest(
    [Required, StringLength(64, MinimumLength = 3)] string Username,
    [Required, StringLength(128, MinimumLength = 1)] string FullName,
    [Required, StringLength(128, MinimumLength = 8)] string Password,
    string Role = "Teacher");

public record CreateStudentRequest(
    [Required, StringLength(128, MinimumLength = 1)] string FullName,
    [Required, StringLength(64, MinimumLength = 1)] string StudentCode,
    [StringLength(32)] string? Phone);

public record UpdateStudentRequest(
    [Required, StringLength(128, MinimumLength = 1)] string FullName,
    [StringLength(32)] string? Phone,
    bool IsActive);

public record CreateMeetingRequest(
    [Required, StringLength(128, MinimumLength = 1)] string CourseName,
    [Required] DateOnly Date,
    [Required] TimeOnly StartTime,
    [Required] TimeOnly EndTime);

public record ScanRequest(
    [Range(1, int.MaxValue)] int MeetingId,
    [Required, StringLength(256, MinimumLength = 1)] string Token);

public record ScanResponse(string Status, string Message, string? StudentName, int? StudentId, int? MeetingId, DateTime? ScannedAt);
