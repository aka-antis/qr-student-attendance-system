# QR Student Attendance System

A QR-code based student attendance system. Each student gets a printable QR card containing
an opaque random token — no personal data is encoded in the QR itself. A teacher scans the
card with the Flutter mobile app, the API resolves the student server-side from the token,
and records attendance for the selected class meeting.

## Features

- Student management with unique student codes.
- Per-student QR tokens: cryptographically random, revocable, regenerable.
- Printable QR cards (QR image + student info rendered outside the QR).
- Class meetings with course name, date, start and end time.
- Teacher mobile flow: login, pick a meeting, scan, instant success/duplicate/error feedback.
- Attendance records stamped with the server's current time and the scanning teacher.
- Duplicate protection at the database level (`Student + Meeting` unique index).
- Attendance reports: per-meeting list, per-student history, attendance percentage, CSV export.
- Web dashboard for administration and QR card printing.
- JWT authentication with Teacher and Admin roles.

## Repository structure

```
qr-student-attendance-system/
  api/                      # ASP.NET Core Web API (.NET 10)
    Controllers/            # Auth, Teachers, Students, Meetings, Attendance, Reports
    Models/                 # Teacher, Student, QrToken, Meeting, AttendanceRecord
    Data/                   # AppDbContext + EF Core migrations
    Services/               # Secure token generation, QR rendering, JWT issuance
    Dtos/                   # Request/response contracts
    wwwroot/dashboard.html  # Admin dashboard + QR card printing
  teacher_app/              # Flutter teacher app
    lib/
      main.dart
      api_client.dart
      screens/              # login, meetings, scanner, history
```

## Tech stack

| Layer      | Technology                                                     |
| ---------- | -------------------------------------------------------------- |
| Backend    | ASP.NET Core Web API, Entity Framework Core                    |
| Database   | SQL Server (production) / SQLite (local development fallback)  |
| QR         | QRCoder (backend PNG generation), `mobile_scanner` (Flutter)   |
| Auth       | JWT Bearer, BCrypt password hashing                            |
| Mobile     | Flutter (`http`, `mobile_scanner`, `shared_preferences`)       |

## Getting started

### Prerequisites

- .NET 10 SDK
- SQL Server (optional — SQLite is used by default for local development)
- Flutter SDK 3.4+ (for the mobile app)

### Run the API

```powershell
cd api
dotnet run --urls "http://localhost:5189"
```

- Dashboard: `http://localhost:5189/dashboard.html`
- OpenAPI document (Development environment): `http://localhost:5189/openapi/v1.json`

On first start the database is created automatically and two demo accounts are seeded
(change or remove them in production):

| Username  | Password    | Role    |
| --------- | ----------- | ------- |
| admin     | admin123    | Admin   |
| teacher   | teacher123  | Teacher |

### Run the teacher app

```bash
cd teacher_app
flutter pub get
flutter run
```

Set the API base URL in `lib/main.dart` before running:

- Android emulator: `http://10.0.2.2:5189`
- Physical device: `http://<your-PC-LAN-IP>:5189`

Typical session: log in, select the current meeting, scan a student's QR card,
and confirm the student's name on the success message.

## How attendance works

1. An admin creates a student. The API issues a random token and renders its QR code.
2. The student receives a printed QR card (QR image plus name/code printed alongside it).
3. An admin or teacher creates a meeting (course, date, start/end time).
4. The teacher opens the meeting in the mobile app and scans QR cards.
5. The app sends only `{ meetingId, token }` to `POST /api/attendance/scan`.
6. The API looks up the active token, resolves the student, and inserts an attendance
   row stamped with the server time. A repeat scan of the same student in the same
   meeting returns `409 duplicate` instead of creating a second row.

## QR cards

- `GET /api/students/{id}/qr` returns the active token, student info, and a Base64 PNG.
- `GET /api/students/{id}/qr.png` returns the QR image directly for download.
- `POST /api/students/{id}/regenerate` revokes the old token and issues a new one.
- `POST /api/students/{id}/revoke` revokes all active tokens for the student.
- The dashboard (`dashboard.html`) lists all students with their QR cards and offers
  one-click printing.

## API reference

All endpoints except `POST /api/auth/login` require a JWT Bearer token.

| Method | Endpoint                              | Description                                  |
| ------ | ------------------------------------- | -------------------------------------------- |
| POST   | `/api/auth/login`                     | Log in, receive JWT                          |
| GET    | `/api/auth/me`                        | Current teacher profile                      |
| GET    | `/api/teachers`                       | List teachers                                |
| POST   | `/api/teachers`                       | Create teacher (Admin)                       |
| GET    | `/api/students?search=`               | List/search students                         |
| POST   | `/api/students`                       | Create student + issue first token           |
| GET    | `/api/students/{id}`                  | Student details                              |
| PUT    | `/api/students/{id}`                  | Update student                               |
| DELETE | `/api/students/{id}`                  | Delete student (Admin)                       |
| GET    | `/api/students/{id}/qr`               | Active token + QR image (Base64)             |
| GET    | `/api/students/{id}/qr.png`           | QR image download                            |
| POST   | `/api/students/{id}/regenerate`       | Revoke old token, issue a new one            |
| POST   | `/api/students/{id}/revoke`           | Revoke all active tokens                     |
| GET    | `/api/meetings`                       | List meetings with attendance counts         |
| POST   | `/api/meetings`                       | Create meeting                               |
| DELETE | `/api/meetings/{id}`                  | Delete meeting (Admin)                       |
| POST   | `/api/attendance/scan`                | Record attendance from a QR token            |
| GET    | `/api/attendance`                     | Search/filter attendance records             |
| GET    | `/api/reports/meeting/{id}`           | Attendance list for a meeting                |
| GET    | `/api/reports/student/{id}`           | Student history + attendance percentage      |
| GET    | `/api/reports/meeting/{id}/export.csv`| Export a meeting's attendance as CSV         |
| GET    | `/api/reports/export.csv`             | Export filtered attendance as CSV            |

### Scan responses

| HTTP | `status`    | Meaning                                        |
| ---- | ----------- | ---------------------------------------------- |
| 200  | `success`   | Attendance recorded; response includes the student name |
| 409  | `duplicate` | Student already recorded for this meeting      |
| 400  | `error`     | Invalid, revoked, or empty QR token            |
| 404  | `error`     | Meeting not found                              |

## Configuration

`api/appsettings.json`:

```json
{
  "Database": { "Provider": "Sqlite" },
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=AttendanceQr;Trusted_Connection=True;TrustServerCertificate=True;",
    "Sqlite": "Data Source=attendance.db"
  },
  "Jwt": {
    "Key": "CHANGE-ME-TO-A-LONG-RANDOM-SECRET-AT-LEAST-32-CHARS!!",
    "Issuer": "AttendanceQr",
    "Audience": "AttendanceQr",
    "ExpiresMinutes": "720"
  }
}
```

- Set `Database:Provider` to `SqlServer` and fill in `ConnectionStrings:Default` for production.
- Always replace `Jwt:Key` with a long random secret in production.
- Database migrations live in `api/Migrations` and are applied automatically at startup.

## Database schema

- `Teachers`: login accounts with `Teacher` or `Admin` roles.
- `Students`: student profiles with a unique `StudentCode`.
- `QrTokens`: `Token → Student` mapping. Unique index on `Token`; only active tokens are accepted for scans.
- `Meetings`: course name, date, start/end time, creating teacher.
- `AttendanceRecords`: student, meeting, server timestamp, scanning teacher. Unique constraint on `(StudentId, MeetingId)` prevents double registration even under concurrent scans.

## Security notes

- QR codes contain only the random token — never names, IDs, or phone numbers.
- The mobile app never sends a student identity; the backend resolves it from the token.
- Only active, non-revoked tokens belonging to active students are accepted.
- Passwords are BCrypt-hashed; API authorization is enforced per endpoint with role checks.
