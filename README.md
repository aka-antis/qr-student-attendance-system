# QR Student Attendance System

A QR-code based student attendance system. Each student gets a printable QR card containing
an opaque random token — no personal data is encoded in the QR itself. A teacher scans the
card with the Flutter mobile app, the API resolves the student server-side from the token,
and records attendance for the selected class meeting.

## Architecture

```mermaid
flowchart LR
    subgraph Admin["Admin (web dashboard)"]
        D[dashboard.html]
    end
    subgraph Teacher["Teacher (Flutter app)"]
        A[Login] --> B[Meetings] --> C[Scanner] --> H[Meeting report]
    end
    subgraph API["ASP.NET Core API"]
        AUTH[Auth/JWT] --> SCAN[POST /attendance/scan]
        SCAN -->|token lookup| TOK[(QrTokens)]
        TOK -->|student| STU[(Students)]
        SCAN -->|insert + unique guard| ATT[(AttendanceRecords)]
        MTG[(Meetings)] --- ATT
    end
    D -->|admin JWT| API
    A -->|teacher JWT| API
    C -->|{meetingId, token} only| SCAN
    QR[Printed QR card<br/>random token only] --> C
```

Key principle: the QR holds only an opaque random token. The mobile app never sends a
student identity — the backend resolves the student from the active token, stamps the
server time, and enforces one record per `Student + Meeting` with a database unique index.

## Repository structure

```
qr-student-attendance-system/
  api/                           # ASP.NET Core Web API (.NET 10)
    Controllers/                 # Auth, Teachers, Students, Meetings, Attendance, Reports
    Models/                      # Teacher, Student, QrToken, Meeting, AttendanceRecord
    Data/                        # AppDbContext + EF Core migrations
    Services/                    # Secure token generation, QR rendering, JWT issuance
    Dtos/                        # Validated request/response contracts
    wwwroot/dashboard.html       # Admin dashboard + QR card printing
    appsettings.json             # Default settings (secrets come from environment variables)
    appsettings.Production.json # Production template (HTTPS + SQL Server, no secrets)
  teacher_app/                   # Flutter teacher app
    lib/
      main.dart
      api_client.dart            # JWT in flutter_secure_storage
      screens/                   # login, meetings, scanner, history
```

## Tech stack

| Layer      | Technology                                                     |
| ---------- | -------------------------------------------------------------- |
| Backend    | ASP.NET Core Web API, Entity Framework Core                    |
| Database   | SQL Server (production) / SQLite (local development fallback)  |
| QR         | QRCoder (backend PNG generation), `mobile_scanner` (Flutter)   |
| Auth       | JWT Bearer, BCrypt password hashing                            |
| Mobile     | Flutter (`http`, `mobile_scanner`, `flutter_secure_storage`)   |

## Setup

### Prerequisites

- .NET 10 SDK
- SQL Server (optional — SQLite is used by default for local development)
- Flutter SDK 3.4+ (for the mobile app)

### 1. Run the API (development)

```powershell
cd api
dotnet run --urls "http://localhost:5189"
```

- Dashboard: `http://localhost:5189/dashboard.html`
- OpenAPI document (Development only): `http://localhost:5189/openapi/v1.json`

On first start the database is created via EF Core migrations. Demo accounts are seeded
**in Development only** (`SeedDemoUsers` defaults to true locally, false in production):

> **Development-only demo credentials — never enabled in production:**
>
> | Username  | Password    | Role    |
> | --------- | ----------- | ------- |
> | admin     | admin123    | Admin   |
> | teacher   | teacher123  | Teacher |

The local dev JWT key lives in `api/appsettings.Development.json`, which is git-ignored
and never committed.

### 2. Run the teacher app

```bash
cd teacher_app
flutter pub get
flutter run -d edge     # or: flutter run -d windows, or a connected phone
flutter test            # widget smoke test
```

Set the API base URL in the app's login screen:

- Web/Windows: `http://localhost:5189`
- Android emulator: `http://10.0.2.2:5189`
- Physical device: `http://<your-PC-LAN-IP>:5189` (same Wi-Fi, API still running)

Typical session: log in, select the current meeting, scan a student's QR card
(or paste a token into the manual entry field), and confirm the student's name
on the success message.

### 3. Production deployment

1. Publish the API behind HTTPS (reverse proxy or platform TLS; the app enforces
   HSTS + HTTPS redirection outside Development).
2. Use SQL Server: set `Database:Provider` to `SqlServer` (see
   `api/appsettings.Production.json`). The schema is created automatically on
   first boot; later schema changes should be applied as reviewed SQL generated
   with `dotnet ef migrations script` (run with `Database__Provider=SqlServer`),
   because the bundled migrations are scaffolded for the SQLite dev loop.
3. Provide secrets **only via environment variables** (never in committed files):
   - `ConnectionStrings__Default` = SQL Server connection string
   - `Jwt__Key` = long random secret (≥ 32 chars; the app refuses to start in
     Production without it)
4. Restrict browsers via `Cors:AllowedOrigins` (exact origins, e.g.
   `["https://your-teacher-app-host.example.com"]`). Mobile apps are unaffected by CORS.
5. Ensure `SeedDemoUsers` is **not** enabled in production (default off).
6. Optionally enforce class-time scanning with `Attendance:EnforceMeetingWindow=true`
   (plus `Attendance:GraceMinutes`, default 15).

## How attendance works

1. An admin creates a student. The API issues a random token and renders its QR code.
2. The student receives a printed QR card (QR image plus name/code printed alongside it).
3. A teacher (or admin) creates a meeting (course, date, start/end time).
4. The teacher opens the meeting in the mobile app and scans QR cards.
5. The app sends only `{ meetingId, token }` to `POST /api/attendance/scan`.
6. The API looks up the active token, resolves the student, optionally checks the
   meeting time window, and inserts an attendance row stamped with the server time.
   A repeat scan of the same student in the same meeting returns `409 duplicate`
   instead of creating a second row.

## QR cards

- `GET /api/students/{id}/qr` (Admin) returns the active token, student info, and a Base64 PNG.
- `GET /api/students/{id}/qr.png` (Admin) returns the QR image directly for download.
- `POST /api/students/{id}/regenerate` (Admin) revokes the old token and issues a new one.
- `POST /api/students/{id}/revoke` (Admin) revokes all active tokens for the student.
- The dashboard lists all students with their QR cards and offers one-click printing.
- Raw tokens never appear in student list/detail responses — only the QR endpoints
  (and the create/regenerate responses) expose them.

## API overview

All endpoints except `POST /api/auth/login` require a JWT Bearer token.
`A` = Admin, `T` = Teacher.

| Method | Endpoint                              | Roles | Description                                  |
| ------ | ------------------------------------- | ----- | -------------------------------------------- |
| POST   | `/api/auth/login`                     | —     | Log in, receive JWT                          |
| GET    | `/api/auth/me`                        | A, T  | Current teacher profile                      |
| GET    | `/api/teachers`                       | A     | List teachers                                |
| POST   | `/api/teachers`                       | A     | Create teacher                               |
| PUT    | `/api/teachers/{id}/deactivate`       | A     | Deactivate teacher                           |
| GET    | `/api/students?search=`               | A     | List/search students (no raw tokens)         |
| POST   | `/api/students`                       | A     | Create student + issue first token           |
| GET    | `/api/students/{id}`                  | A     | Student details (no raw token)               |
| PUT    | `/api/students/{id}`                  | A     | Update student                               |
| DELETE | `/api/students/{id}`                  | A     | Delete student                               |
| GET    | `/api/students/{id}/qr`               | A     | Active token + QR image (Base64)             |
| GET    | `/api/students/{id}/qr.png`           | A     | QR image download                            |
| POST   | `/api/students/{id}/regenerate`       | A     | Revoke old token, issue a new one            |
| POST   | `/api/students/{id}/revoke`           | A     | Revoke all active tokens                     |
| GET    | `/api/meetings`                       | A, T  | List meetings with attendance counts         |
| GET    | `/api/meetings/{id}`                  | A, T  | Meeting details                              |
| POST   | `/api/meetings`                       | A, T  | Create meeting                               |
| DELETE | `/api/meetings/{id}`                  | A     | Delete meeting                               |
| POST   | `/api/attendance/scan`                | A, T  | Record attendance from a QR token            |
| GET    | `/api/attendance`                     | A     | Search/filter attendance records             |
| DELETE | `/api/attendance/{id}`                | A     | Delete an attendance record                  |
| GET    | `/api/reports/meeting/{id}`           | A, T  | Attendance list for a meeting                |
| GET    | `/api/reports/student/{id}`           | A     | Student history + attendance percentage      |
| GET    | `/api/reports/meeting/{id}/export.csv`| A     | Export a meeting's attendance as CSV         |
| GET    | `/api/reports/export.csv`             | A     | Export filtered attendance as CSV            |

Teachers hold the minimum needed to run a class: log in, list/create meetings,
scan QR codes, and view the current meeting's attendance list. Everything else —
students, QR lifecycle, teacher accounts, deletions, student histories, exports —
is Admin-only and enforced server-side (including the `Student + Meeting` unique
index, which even guards against concurrent duplicate scans).

### Scan responses

| HTTP | `status`    | Meaning                                                  |
| ---- | ----------- | -------------------------------------------------------- |
| 200  | `success`   | Attendance recorded; response includes the student name   |
| 409  | `duplicate` | Student already recorded for this meeting                 |
| 400  | `error`     | Invalid/revoked/empty token, or outside meeting window    |
| 404  | `error`     | Meeting not found                                         |

Validation failures return `400` with a consistent `{ message, errors }` shape.

## Configuration reference (`api/appsettings.json`)

| Key                              | Default     | Notes                                                        |
| -------------------------------- | ----------- | ------------------------------------------------------------ |
| `Database:Provider`              | `Sqlite`    | `SqlServer` in production                                    |
| `ConnectionStrings:Default`      | local       | Override via `ConnectionStrings__Default` env var            |
| `Jwt:Key`                        | placeholder | Override via `Jwt__Key` env var / dev-only file; required in Production |
| `Cors:AllowedOrigins`            | `[]`        | Exact browser origins; dev-only fallback to allow-any        |
| `Attendance:EnforceMeetingWindow`| `false`     | Reject scans outside `[start−grace, end+grace]` (server-local time) |
| `Attendance:GraceMinutes`        | `15`        | Grace period around the meeting window                       |
| `SeedDemoUsers`                  | dev-only    | Never enable in production                                   |

## Security notes

- QR codes contain only the random token — never names, IDs, or phone numbers.
- The mobile app never sends a student identity; the backend resolves it from the token.
- Only active, non-revoked tokens belonging to active students are accepted.
- Passwords are BCrypt-hashed; authorization is enforced per endpoint with role checks.
- JWTs are stored in encrypted platform storage on mobile (`flutter_secure_storage`).
- The dashboard escapes all server-provided strings before rendering (no raw `innerHTML`).
- No secrets are committed: dev key lives in git-ignored `appsettings.Development.json`,
  production secrets come from environment variables.
