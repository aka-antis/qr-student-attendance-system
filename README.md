# QR Student Attendance System

A QR-code based attendance system for schools and training centers. Each student receives a
printed QR card, teachers scan cards with a mobile app, and attendance is recorded
automatically — no paper registers, no manual data entry.

Built with **ASP.NET Core** (C#) and **Flutter** (Dart).

## Screenshots / Demo

<!-- Add dashboard screenshot here: docs/screenshots/dashboard.png -->
<!-- Add teacher app login screenshot here: docs/screenshots/app-login.png -->
<!-- Add QR scanner screenshot here: docs/screenshots/app-scanner.png -->
<!-- Add attendance history screenshot here: docs/screenshots/app-history.png -->

## Features

- Student management (create, update, deactivate, search)
- Unique QR code per student, backed by a secure random token
- Printable QR cards with QR image plus student info
- QR token regeneration and revocation
- Teacher authentication with Admin and Teacher roles
- Class/meeting management (course, date, start/end time)
- QR attendance scanning from the mobile app
- Automatic server-side attendance timestamps
- Duplicate attendance prevention (database unique constraint)
- Attendance reports and per-student history with percentages
- CSV export of attendance records
- Web dashboard for administration and card printing

## How It Works

1. An admin registers a student and the system issues a random QR token.
2. The student receives a printed QR card.
3. The teacher selects the current class meeting in the mobile app and scans the card.
4. The API resolves the student from the token — the app never sends a student identity.
5. Attendance is recorded with the server's current time.
6. Reports and attendance percentages update immediately.

## Security

- QR codes contain only an opaque random token (32 bytes from a cryptographic random
  generator) — no names, IDs, or phone numbers.
- Raw tokens are exposed only through the QR issuing endpoints, never in list responses.
- Authentication uses JWT Bearer tokens; passwords are BCrypt-hashed.
- Role-based authorization: teachers can only run classes (meetings, scanning, meeting
  lists); student, QR, teacher-account, deletion, history, and export operations are
  Admin-only.
- Duplicate attendance is enforced by a database-level unique constraint, including
  under concurrent scans.

## Tech Stack

Backend:

- C#, ASP.NET Core Web API
- Entity Framework Core
- SQL Server (production), SQLite (local development)
- JWT Bearer authentication, BCrypt password hashing
- QRCoder (QR image generation)

Mobile:

- Flutter, Dart
- mobile_scanner (QR camera scanning)
- http (API calls), flutter_secure_storage (encrypted JWT storage)

Frontend:

- Vanilla HTML, CSS, and JavaScript (admin dashboard, no frameworks)

## Project Structure

```
qr-student-attendance-system/
  api/                      # ASP.NET Core Web API
    Controllers/            # Auth, Teachers, Students, Meetings, Attendance, Reports
    Models/                 # Teacher, Student, QrToken, Meeting, AttendanceRecord
    Data/                   # EF Core DbContext and migrations
    Services/               # Token generation, QR rendering, JWT issuance
    wwwroot/dashboard.html  # Admin dashboard and QR card printing
  teacher_app/              # Flutter teacher app
    lib/screens/            # Login, meetings, scanner, history
```

## API

Authenticated with JWT Bearer tokens (teachers log in via the mobile app or dashboard).
The API surface covers authentication, teachers, students and their QR lifecycle,
meetings, attendance scanning, reports, and CSV export. In Development the machine-readable
API specification is served at `/openapi/v1.json`.

## Database

- **Teachers** — login accounts with Admin or Teacher roles.
- **Students** — profiles with a unique student code.
- **QrTokens** — token-to-student mapping; unique token index, one active token per student.
- **Meetings** — course name, date, start/end time, creating teacher.
- **AttendanceRecords** — student, meeting, server timestamp, scanning teacher, with a
  unique constraint on student plus meeting.

## Running Locally

Prerequisites:

- .NET 10 SDK
- Flutter SDK 3.4+ (for the mobile app)
- SQL Server is optional — SQLite is used by default for local development

```powershell
# 1. Clone the repository
git clone https://github.com/aka-antis/qr-student-attendance-system.git
cd qr-student-attendance-system

# 2. Start the API (database is created automatically on first run)
cd api
dotnet run --urls "http://localhost:5189"
```

Dashboard: `http://localhost:5189/dashboard.html`

Connection strings and JWT settings live in `api/appsettings.json` (defaults) and
environment-specific files. Key settings:

| Setting | Purpose |
| ------- | ------- |
| `Database:Provider` | `Sqlite` (default) or `SqlServer` |
| `ConnectionStrings:Default` | SQL Server connection string (production via `ConnectionStrings__Default` env var) |
| `Jwt:Key` | Signing key (production via `Jwt__Key` env var, required) |
| `Cors:AllowedOrigins` | Allowed browser origins |
| `Attendance:EnforceMeetingWindow` | Optionally reject scans outside class time |

Run the Flutter app:

```bash
cd teacher_app
flutter pub get
flutter run -d edge   # or a connected device; set the API base URL on the login screen
```

## Demo Credentials

> **DEVELOPMENT / DEMO ONLY — seeded automatically in Development builds.
> Change or remove these accounts before any production use.**

| Username  | Password    | Role    |
| --------- | ----------- | ------- |
| admin     | admin123    | Admin   |
| teacher   | teacher123  | Teacher |

## Production / Security Notes

- Replace the JWT secret (`Jwt__Key` environment variable, at least 32 characters) —
  the app refuses to start in Production without it.
- Remove or replace the demo credentials; never enable demo seeding in production.
- Use SQL Server with a real connection string and serve the API behind HTTPS.
- Configure `Cors:AllowedOrigins` with the exact production origins.

## Future Improvements

- Push notifications for absence alerts
- Attendance analytics and charts
- Additional export formats (Excel/PDF)
- Bulk student import
- Deployment templates for one-click hosting
