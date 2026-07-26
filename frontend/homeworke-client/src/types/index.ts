// ── Auth ──────────────────────────────────────────
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  departmentId?: number;
}

export interface LoginResponse {
  token: string;
  fullName: string;
  role: UserRole;
  employeeCode: string;
}

export type UserRole = 'Employee' | 'Manager' | 'Admin';

// ── Attendance ────────────────────────────────────
export interface ClockRequest {
  notes?: string;
}

export interface AttendanceRecord {
  id: number;
  employeeName: string;
  employeeCode: string;
  shiftDate: string;
  clockIn: string;
  clockOut: string | null;
  hoursWorked: number | null;
  status: string;
  isOpen: boolean;
  timeApiFailed: boolean;
  isManuallyAdjusted: boolean;
  notes: string | null;
}

export interface AttendanceStatus {
  isClockedIn: boolean;
  message?: string;
  record?: AttendanceRecord;
}

export interface AttendanceSummary {
  employeeName: string;
  employeeCode: string;
  date: string;
  clockIn: string | null;
  clockOut: string | null;
  hoursWorked: number | null;
  status: string;
}

export interface DailyReport {
  date: string;
  totalEmployees: number;
  presentCount: number;
  absentCount: number;
  completedCount: number;
  averageHours: number;
  records: AttendanceSummary[];
}

export interface MonthlyReport {
  year: number;
  month: number;
  employeeName: string;
  employeeCode: string;
  daysWorked: number;
  daysAbsent: number;
  daysLate: number;
  totalHours: number;
  averageDailyHours: number;
}

// ── Admin ─────────────────────────────────────────
export interface AdminAdjustmentRequest {
  attendanceRecordId: number;
  newClockIn?: string;
  newClockOut?: string;
  reason: string;
}

export interface EmployeeDto {
  id: number;
  employeeCode: string;
  fullName: string;
  email: string;
  department: string;
  role: string;
  isActive: boolean;
  lastLoginAt: string | null;
  managerId: number | null;
  managerName: string | null;
}

// ── Reports: Current Status & History ───────────

export interface EmployeeStatus {
  employeeName: string;
  employeeCode: string;
  department: string;
  isWorking: boolean;
  clockIn: string | null;
  clockOut: string | null;
  hoursWorkedToday: number | null;
}

export interface CurrentStatusResponse {
  totalEmployees: number;
  workingNow: number;
  notWorking: number;
  employees: EmployeeStatus[];
}

export interface PaginatedHistory {
  totalCount: number;
  page: number;
  pageSize: number;
  records: AttendanceRecord[];
}

export interface AuditLogEntry {
  id: number;
  entityName: string;
  entityId: number;
  action: string;
  performedByEmployeeId: number | null;
  oldValue: string | null;
  newValue: string | null;
  ipAddress: string | null;
  timestamp: string;
}

// ── Common ────────────────────────────────────────
export interface ApiError {
  error: string;
  detail?: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
