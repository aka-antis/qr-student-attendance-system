# Teacher App (Flutter)

Teacher mobile app for QR attendance.

## Setup

1. Install Flutter SDK (>=3.4).
2. Update API base URL in `lib/main.dart`:
   - Emulator: `http://10.0.2.2:5189`
   - Physical device: `http://<PC-LAN-IP>:5189`
3. Run:

```bash
cd teacher_app
flutter pub get
flutter run
```

## Flow

1. Login (teacher/teacher123 or admin/admin123).
2. Select current meeting.
3. Open scanner → scan student QR → app sends ONLY `{meetingId, token}`.
4. Backend resolves student, records server-time attendance.
5. UI shows:
   - green success + student name,
   - orange "already registered",
   - red "invalid/revoked".

## Packages

- `http` – API calls
- `mobile_scanner` – QR camera scanning
- `flutter_secure_storage` – encrypted JWT storage
