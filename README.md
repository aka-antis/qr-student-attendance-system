# QR Student Attendance System

Stack: ASP.NET Core Web API + EF Core (SQL Server / SQLite fallback) + Flutter teacher app.

## What was found in C:\coding

No existing .NET/Flutter attendance code. Existing folders are unrelated:
`cpp/` demo, `Java/` test, `Python/` scripts, `web/` empty, `sql/` Oracle restaurant demo,
`sqldeveloper/` IDE install, `mullem/` Django e-learning platform, `sadda/` Tauri music player.
So this is a greenfield project at `C:\coding\attendance-qr\`.

## Structure

```
attendance-qr/
  api/                 # ASP.NET Core Web API
    Models/            # Teacher, Student, QrToken, Meeting, AttendanceRecord
    Data/AppDbContext  # Unique(Student+Meeting), Unique(Token), Unique(StudentCode)
    Services/          # SecureTokenService (RandomNumberGenerator 256-bit),
                       # QrCodeService (QRCoder, QR holds ONLY token), JwtService
    Controllers/       # auth, teachers, students (+qr/regenerate/revoke),
                       # meetings, attendance/scan, reports (+CSV export)
    Migrations/        # EF Core InitialCreate
    wwwroot/dashboard.html  # admin UI + printable QR cards
  teacher_app/         # Flutter app (login → meetings → scanner → history)
```

## Security model

- QR = opaque 43-char Base64Url token (32 random bytes). No name/ID/phone inside.
- Mobile sends ONLY `{meetingId, token}`. Student resolved server-side via active token.
- JWT auth on all endpoints. Passwords BCrypt-hashed. Seed: `admin/admin123`, `teacher/teacher123`.
- Duplicate prevention: friendly 409 check + DB unique index on `(StudentId, MeetingId)` + race-safe catch.
- `ScannedAt = DateTime.UtcNow` server time. `ScannedByTeacherId` from JWT.

## Run API

```powershell
cd C:\coding\attendance-qr\api
dotnet run --urls "http://localhost:5189"
# Dashboard: http://localhost:5189/dashboard.html
# OpenAPI:   http://localhost:5189/openapi/v1.json (Development)
```

SQLite is default (`Database:Provider=Sqlite`). For SQL Server:
`"Database:Provider": "SqlServer"` + set `ConnectionStrings:Default`
to e.g. `Server=localhost;Database=AttendanceQr;Trusted_Connection=True;TrustServerCertificate=True;`

## Run Flutter app

Flutter SDK not installed on this machine – source is generated, not executed.
```bash
cd teacher_app
flutter pub get
flutter run
```
Set base URL in `lib/main.dart` (`10.0.2.2` for emulator, LAN IP for device).

## Verified end-to-end (2026-10-07)

`ALL_E2E_PASSED` via live API on :5189:
login → create student → get QR token → create meeting → scan success (student name)
→ duplicate 409 → invalid 400 → meeting/student reports + percentage → CSV export
→ regenerate revokes old token.
