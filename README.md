# 🕐 HomeWorke — Time & Attendance System

A full-stack time and attendance system with **React** frontend and **ASP.NET Core** backend, using **SQL Server** (production) or **SQLite** (development) for persistence. All clock-in/out times are validated against an **external API** for the **Europe/Zurich** timezone — never relying on browser or local server time.

---

## 🏗 Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Frontend (React)                      │
│  Vite · TypeScript · TailwindCSS · React Router          │
│  Port: 5173 (dev)                                       │
└──────────────────────┬──────────────────────────────────┘
                       │ HTTP (JWT Bearer)
┌──────────────────────▼──────────────────────────────────┐
│               Backend (ASP.NET Core 8)                   │
│  Controllers · Services · EF Core · JWT Auth             │
│  Polly Resilience · Swagger                              │
│  Port: 5001                                              │
└──────┬───────────────────────────────┬──────────────────┘
       │                               │
       ▼                               ▼
┌──────────────────┐   ┌──────────────────────────┐
│ SQL Server /     │   │  WorldTimeAPI.org         │
│ SQLite (dev)     │   │  /api/timezone/           │
│  HomeWorkeDb     │   │  /api/timezone/           │
│                  │   │  Europe/Zurich            │
└────────────----──┘   └──────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- SQL Server (optional — SQLite is used automatically for development)

### 1. Database

**Development (zero setup):** The app auto-creates a SQLite database on first run. No configuration needed.

**Production (SQL Server):** Set `"UseSqlite": false` in `appsettings.json` and update the connection string:

```bash
# Docker: quick SQL Server
  docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" \
    -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

# Or use the raw SQL init script
sqlcmd -S localhost -i database/init.sql
```

### 2. Backend

```bash
cd backend/HomeWorke.Api
dotnet restore
dotnet ef database update   # Apply migrations
dotnet run                  # Starts on http://localhost:5001
```

Swagger UI available at: http://localhost:5001/swagger

### 3. Frontend

```bash
cd frontend/homeworke-client
npm install
npm run dev                 # Starts on http://localhost:5173
```

### 4. Login

| Role  | Email                  | Password   |
|-------|------------------------|------------|
| Admin | admin@homeworke.com    | Admin@123  |

---

## 🔑 Key Features

### ⏱ External Time Validation
Every Clock In / Clock Out operation queries **WorldTimeAPI.org** for the current time in **Europe/Zurich**. The browser clock and server local time are **never** used for attendance records.

### 🔒 Business Rules & Edge Cases

| Scenario | Behavior |
|---|---|
| **Double Clock-In** | Rejected — must clock out first |
| **Clock-Out without Clock-In** | Rejected — no open shift found |
| **Time API failure** | Retries 3x with exponential backoff, then circuit breaker opens for 30s |
| **Midnight-crossing shifts** | Clock-out date may differ from shift date — handled correctly |
| **Shifts > 14 hours** | Auto-flagged for admin review with warning |
| **Admin adjustment** | Full audit trail with old/new values stored as JSON |
| **Inactive employees** | Cannot log in |

### 📊 Reports (Manager/Admin)
- **Daily Report**: All employees' attendance for a specific date
- **Monthly Report**: Per-employee summary (days worked, absent, late, total hours)

### 🛡 Admin Panel
- View all employees and their status
- Full audit log with pagination
- Adjust attendance records with mandatory reason

---

## 📁 Project Structure

```
HomeWorke/
├── backend/
│   └── HomeWorke.Api/
│       ├── Controllers/          # Auth, Attendance, Reports, Admin
│       ├── Models/
│       │   ├── Domain/           # Employee, AttendanceRecord, etc.
│       │   ├── DTOs/             # Request/response types
│       │   └── Enums/            # Status, Role, RecordType
│       ├── Services/             # Business logic
│       │   ├── ITimeService.cs   # Zurich time via external API
│       │   ├── AuthService.cs    # JWT authentication
│       │   └── AttendanceService.cs  # Clock in/out logic
│       ├── Data/                 # EF Core DbContext
│       ├── Middleware/           # Global exception handling
│       └── Program.cs            # App configuration
├── frontend/
│   └── homeworke-client/
│       ├── src/
│       │   ├── components/       # Reusable UI components
│       │   ├── pages/            # Route pages
│       │   ├── services/         # API client
│       │   ├── hooks/            # Custom React hooks
│       │   ├── context/          # Auth context
│       │   └── types/            # TypeScript interfaces
│       └── package.json
└── database/
    └── init.sql                  # Raw SQL schema + seed data
```

---

## 🔐 API Endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | — | Login, get JWT |
| POST | `/api/auth/change-password` | Any | Change password |
| POST | `/api/attendance/clock-in` | Any | Clock in (Zurich time) |
| POST | `/api/attendance/clock-out` | Any | Clock out (Zurich time) |
| GET | `/api/attendance/status` | Any | Current shift status |
| GET | `/api/attendance/history` | Any | Personal history |
| GET | `/api/reports/daily` | Manager+ | Daily report |
| GET | `/api/reports/monthly` | Manager+ | Monthly summary |
| PUT | `/api/admin/adjust-attendance` | Admin | Correct records |
| GET | `/api/admin/employees` | Admin | List employees |
| GET | `/api/admin/audit-log` | Admin | Audit trail |

---

## 🧠 Design Decisions

1. **Time API with 5-second cache**: Prevents hammering the external API during rapid requests while staying accurate
2. **Polly resilience policies**: Retry (3x exponential) + Circuit Breaker (30s) for time API failures
3. **JWT with zero clock skew**: Tokens expire exactly at expiry time — no tolerance
4. **BCrypt password hashing**: Industry-standard adaptive hashing
5. **Filtered index on open records**: `WHERE ClockOut IS NULL` for fast "find active shift" queries
6. **Vite proxy in dev**: `/api` requests proxy to backend — no CORS issues in development

---

## 🔮 Future Enhancements (Not Yet Implemented)

- [ ] Location/geofencing validation for clock-in
- [ ] Email notifications for forgotten clock-outs
- [ ] Leave request workflow (approve/deny)
- [ ] Biometric integration
- [ ] Multi-language support
- [ ] Overtime calculation rules
- [ ] Shift scheduling & roster management
- [ ] Real-time dashboard with SignalR
