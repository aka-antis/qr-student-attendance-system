namespace Api.Dtos;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, string FullName, string Role);
public record CreateTeacherRequest(string Username, string FullName, string Password, string Role = "Teacher");
public record CreateStudentRequest(string FullName, string StudentCode, string? Phone);
public record UpdateStudentRequest(string FullName, string? Phone, bool IsActive);
public record CreateMeetingRequest(string CourseName, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);
public record ScanRequest(int MeetingId, string Token);
public record ScanResponse(string Status, string Message, string? StudentName, int? StudentId, int? MeetingId, DateTime? ScannedAt);
