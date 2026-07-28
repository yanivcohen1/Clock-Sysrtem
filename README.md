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

  sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -C -i database/init.sql

# Or use the raw SQL init script
sqlcmd -S localhost -i database/init.sql
```

### 2. Backend

```bash
cd backend/HomeWorke.Api
dotnet restore
dotnet ef database update   # Apply migrations
dotnet run                  # Starts on http://localhost:5001

# Run tests
cd ../HomeWorke.Api.Tests
dotnet test
```

Swagger UI available at: http://localhost:5001/swagger

### 3. Frontend

```bash
cd frontend/homeworke-client
npm install
npm run dev                 # Starts on http://localhost:5173

# Run tests
npm test                    # Single run
npm run test:watch          # Watch mode
```

### 4. Login

| Role     | Email                   | Password     |
|----------|-------------------------|--------------|
| Admin    | admin@homeworke.com     | Admin@123    |
| Manager  | manager@homeworke.com   | Manager@123  |
| Employee | demo@homeworke.com      | Demo@123     |

---

## 🧪 Testing

### Backend (C# / xUnit)

**33 tests** covering AuthService and AttendanceService using EF Core InMemory database and Moq for mocking.

```bash
cd backend/HomeWorke.Api.Tests
dotnet test
```

| File | Tests | What's covered |
|---|---|---|
| `AuthServiceTests.cs` | 16 | Login (valid/wrong/inactive/unknown), Register (valid/duplicate/dept validation), ChangePassword, ResetPassword (valid/token expiry/short password), ValidateToken |
| `AttendanceServiceTests.cs` | 17 | ClockIn (creates record/prevents double), ClockOut (valid/no active record/before clock-in/14h+ flag), GetCurrentStatus, GetHistory (pagination/date filters), AdminAdjust (audit trail), GetCurrentStatusAll, GetDailyReport |

### Frontend (React / Vitest)

**29 tests** across services, hooks, and components using Vitest with React Testing Library and jsdom.

```bash
cd frontend/homeworke-client
npm test                 # Run once
npm run test:watch       # Watch mode
npm run test:coverage    # With coverage report
```

| File | Tests | What's covered |
|---|---|---|
| `authService.test.ts` | 6 | login, register, changePassword, forgotPassword, resetPassword |
| `attendanceService.test.ts` | 9 | clockIn, clockOut, getStatus, getHistory, getDailyReport, getMonthlyReport, getCurrentStatus |
| `useAttendance.test.ts` | 6 | Hook: default state, fetchStatus (success/error), clockIn (success/error), clockOut |
| `LoginPage.test.tsx` | 6 | Component: renders form, branding, login submission, error display, forgot-password link, demo credentials |
| `api.test.ts` | 2 | Axios instance: base URL, request/response interceptors registered |

---

## 🔑 Key Features

### ⏱ External Time Validation
Every Clock In / Clock Out operation queries **WorldTimeAPI.org** for the current time in **Europe/Zurich**. The browser clock and server local time are **never** used for attendance records.

### 🔒 Business Rules & Edge Cases

| Scenario | Behavior |
|---|---|
| **Double Clock-In** | Rejected — must clock out first |
| **Clock-Out without Clock-In** | Rejected — no open shift found |
| **Time API failure** | Falls back across 3 sources: timeapi.io → WorldTimeAPI HTTP → WorldTimeAPI HTTPS, 8s timeout each |
| **Midnight-crossing shifts** | Clock-out date may differ from shift date — handled correctly |
| **Shifts > 14 hours** | Auto-flagged for admin review with warning |
| **Admin adjustment** | Full audit trail with old/new values stored as JSON |
| **Inactive employees** | Cannot log in |

### 🏢 Manager-Employee Hierarchy
- Each **Employee** and **Manager** can be assigned a parent **Manager**
- **Recursive reporting**: A manager sees not only direct reports but **all subordinates down the entire chain**
- **Admin** sees all employees regardless of hierarchy
- Demo: Manager (`manager@homeworke.com`) manages Employee (`demo@homeworke.com`)

### 📊 Reports (Manager/Admin)
Four views with **10 items per page** and ◀ Prev / Page X of Y / Next ▶ controls:
- **Current Status**: Live view of all employees — who's working right now, with pulse indicator
- **Daily Report**: All employees' attendance for a specific date
- **Monthly Report**: Per-employee summary (days worked, absent, late, total hours)
- **History**: Full attendance log with date range & employee filters

### 🛡 Admin Panel
- View all employees (10 per page) with role, department, status, and **manager assignment**
- **Add Employee** form with role selection, department, and **Manager dropdown** (required for Employee, optional for Manager)
- Full audit log (10 per page, ◀ Prev / Page X of Y / Next ▶)
- Toggle employee active/inactive status
- Reset employee passwords
- Delete employees (hard delete with audit log)
- Adjust attendance records with mandatory reason

### 📄 Pagination
Every table in the application is paginated with a **uniform page size of 10 items**:

| Feature | Location | Controls |
|---|---|---|
| Current Status | Reports | ◀ Prev / Page X of Y / Next ▶ |
| Daily Report | Reports | ◀ Prev / Page X of Y / Next ▶ |
| Monthly Report | Reports | ◀ Prev / Page X of Y / Next ▶ |
| History | Reports | ◀ Prev / Page X of Y / Next ▶ |
| Employees | Admin Panel | ◀ Prev / Page X of Y / Next ▶ |
| Audit Log | Admin Panel | ◀ Prev / Page X of Y / Next ▶ |
| Personal History | My History | ◀ Prev / Page X of Y / Next ▶ |

Backend uses `PaginatedResponse<T>` = `(totalCount, page, pageSize, items)` on all list endpoints.
Frontend uses a consistent `PAGE_SIZE = 10` constant with matching pagination UI.

---

## 📁 Project Structure

```
HomeWorke/
├── backend/
│   ├── HomeWorke.Api/
│   │   ├── Controllers/          # Auth, Attendance, Reports, Admin
│   │   ├── Models/
│   │   │   ├── Domain/           # Employee, AttendanceRecord, etc.
│   │   │   ├── DTOs/             # Request/response types
│   │   │   └── Enums/            # Status, Role, RecordType
│   │   ├── Services/             # Business logic
│   │   │   ├── ITimeService.cs   # Zurich time via external API
│   │   │   ├── AuthService.cs    # JWT authentication
│   │   │   └── AttendanceService.cs  # Clock in/out logic
│   │   ├── Data/                 # EF Core DbContext
│   │   ├── Middleware/           # Global exception handling
│   │   └── Program.cs            # App configuration
│   └── HomeWorke.Api.Tests/      # Unit tests (xUnit + EF Core InMemory + Moq)
│       ├── AuthServiceTests.cs
│       ├── AttendanceServiceTests.cs
│       └── TestDbContextFactory.cs
├── frontend/
│   └── homeworke-client/
│       ├── src/
│       │   ├── components/       # Reusable UI components
│       │   ├── pages/            # Route pages
│       │   ├── services/         # API client
│       │   ├── hooks/            # Custom React hooks
│       │   ├── context/          # Auth context
│       │   ├── types/            # TypeScript interfaces
│       │   └── test/             # Unit tests (Vitest + React Testing Library)
│       ├── package.json
│       └── vite.config.ts        # Vite + Vitest config
└── database/
    └── init.sql                  # Raw SQL schema + seed data
```

---

## 🔐 API Endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | — | Login, get JWT |
| POST | `/api/auth/register` | — | Self-registration |
| POST | `/api/auth/forgot-password` | — | Request password reset |
| POST | `/api/auth/reset-password` | — | Reset with token |
| POST | `/api/auth/change-password` | Any | Change password |
| POST | `/api/attendance/clock-in` | Any | Clock in (Zurich time) |
| POST | `/api/attendance/clock-out` | Any | Clock out (Zurich time) |
| GET | `/api/attendance/status` | Any | Current shift status |
| GET | `/api/attendance/history` | Any | Personal history |
| GET | `/api/reports/daily` | Manager+ | Daily report (filtered by hierarchy) |
| GET | `/api/reports/monthly` | Manager+ | Monthly summary (filtered by hierarchy) |
| GET | `/api/reports/current-status` | Manager+ | Live status of all subordinates |
| GET | `/api/reports/history` | Manager+ | Paginated history with filters |
| PUT | `/api/admin/adjust-attendance` | Admin | Correct records |
| GET | `/api/admin/employees` | Admin | List employees with manager info |
| POST | `/api/admin/employees` | Admin | Create employee (role + managerId) |
| PUT | `/api/admin/employees/{id}/toggle-status` | Admin | Activate/deactivate |
| PUT | `/api/admin/employees/{id}/reset-password` | Admin | Reset password |
| DELETE | `/api/admin/employees/{id}` | Admin | Delete employee |
| GET | `/api/admin/audit-log` | Admin | Audit trail |

---

## 🧠 Design Decisions

1. **Multi-source time API with fallback**: timeapi.io → WorldTimeAPI HTTP → WorldTimeAPI HTTPS, each with fresh HttpClient and 8s timeout. 5-second cache prevents API hammering.
2. **BFS recursive hierarchy**: Manager sees all subordinates at any depth — collected via in-memory BFS for cross-database compatibility.
3. **JWT with zero clock skew**: Tokens expire exactly at expiry time — no tolerance
4. **BCrypt password hashing**: Industry-standard adaptive hashing
5. **Self-referencing FK**: `Employees.ManagerId → Employees.Id` with `ON DELETE RESTRICT`
6. **Vite proxy in dev**: `/api` requests proxy to backend — no CORS issues in development

---

## ✅ Implemented Features
- [x] Multi-source external time validation (timeapi.io + WorldTimeAPI)
- [x] Unlimited daily clock-in/out cycles
- [x] Self-registration with department selection
- [x] Password reset / forgot password flow
- [x] Manager-Employee hierarchy with recursive reporting
- [x] Admin panel: CRUD employees, assign managers, audit log
- [x] Reports: Current Status, Daily, Monthly, History — all with pagination (10/page) & filters
- [x] Role-based access: Employee / Manager / Admin
- [x] Demo seed data: 3 accounts + 32 attendance records + 55 audit log entries
- [x] Consistent pagination: ◀ Prev / Page X of Y / Next ▶ on all tables
- [x] Global page size: 10 items per page across the entire application
- [x] Unit tests: 33 C# backend tests (xUnit) + 29 React frontend tests (Vitest)

## 🔮 Future Enhancements
- [ ] Location/geofencing validation for clock-in
- [ ] Email notifications for forgotten clock-outs
- [ ] Leave request workflow (approve/deny)
- [ ] Biometric integration
- [ ] Multi-language support
- [ ] Overtime calculation rules
- [ ] Shift scheduling & roster management
- [ ] Real-time dashboard with SignalR
