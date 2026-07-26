import api from './api';
import type {
  ClockRequest,
  AttendanceRecord,
  AttendanceStatus,
  DailyReport,
  MonthlyReport,
  AdminAdjustmentRequest,
  EmployeeDto,
  AuditLogEntry,
  CurrentStatusResponse,
  PaginatedHistory,
  PaginatedResponse,
} from '../types';

export const attendanceService = {
  // ── Employee actions ──────────────────────────
  clockIn: async (data: ClockRequest = {}): Promise<AttendanceRecord> => {
    const res = await api.post<AttendanceRecord>('/attendance/clock-in', data);
    return res.data;
  },

  clockOut: async (data: ClockRequest = {}): Promise<AttendanceRecord> => {
    const res = await api.post<AttendanceRecord>('/attendance/clock-out', data);
    return res.data;
  },

  getStatus: async (): Promise<AttendanceStatus> => {
    const res = await api.get<AttendanceStatus>('/attendance/status');
    return res.data;
  },

  getHistory: async (from?: string, to?: string, page = 1, pageSize = 10): Promise<AttendanceRecord[]> => {
    const params = new URLSearchParams();
    if (from) params.set('from', from);
    if (to) params.set('to', to);
    params.set('page', String(page));
    params.set('pageSize', String(pageSize));
    const res = await api.get<AttendanceRecord[]>(`/attendance/history?${params}`);
    return res.data;
  },

  // ── Reports (Manager/Admin) ────────────────────
  getDailyReport: async (date: string, page = 1, pageSize = 10): Promise<DailyReport> => {
    const res = await api.get<DailyReport>(`/reports/daily?date=${date}&page=${page}&pageSize=${pageSize}`);
    return res.data;
  },

  getMonthlyReport: async (year: number, month: number, page = 1, pageSize = 10): Promise<PaginatedResponse<MonthlyReport>> => {
    const res = await api.get<PaginatedResponse<MonthlyReport>>(`/reports/monthly?year=${year}&month=${month}&page=${page}&pageSize=${pageSize}`);
    return res.data;
  },

  getCurrentStatus: async (page = 1, pageSize = 10): Promise<CurrentStatusResponse> => {
    const res = await api.get<CurrentStatusResponse>(`/reports/current-status?page=${page}&pageSize=${pageSize}`);
    return res.data;
  },

  getReportHistory: async (params: {
    employeeId?: number;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedHistory> => {
    const searchParams = new URLSearchParams();
    if (params.employeeId) searchParams.set('employeeId', String(params.employeeId));
    if (params.from) searchParams.set('from', params.from);
    if (params.to) searchParams.set('to', params.to);
    if (params.page) searchParams.set('page', String(params.page));
    if (params.pageSize) searchParams.set('pageSize', String(params.pageSize));
    const res = await api.get<PaginatedHistory>(`/reports/history?${searchParams}`);
    return res.data;
  },

  // ── Admin actions ──────────────────────────────
  adjustAttendance: async (data: AdminAdjustmentRequest): Promise<AttendanceRecord> => {
    const res = await api.put<AttendanceRecord>('/admin/adjust-attendance', data);
    return res.data;
  },

  getEmployees: async (page = 1, pageSize = 10): Promise<PaginatedResponse<EmployeeDto>> => {
    const res = await api.get<PaginatedResponse<EmployeeDto>>(`/admin/employees?page=${page}&pageSize=${pageSize}`);
    return res.data;
  },

  getAuditLog: async (page = 1): Promise<{ total: number; page: number; pageSize: number; logs: AuditLogEntry[] }> => {
    const res = await api.get(`/admin/audit-log?page=${page}`);
    return res.data;
  },

  toggleEmployeeStatus: async (id: number): Promise<{ id: number; isActive: boolean }> => {
    const res = await api.put(`/admin/employees/${id}/toggle-status`);
    return res.data;
  },

  createEmployee: async (data: {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    departmentId?: number;
    role: string;
    managerId?: number;
  }): Promise<EmployeeDto> => {
    const res = await api.post<EmployeeDto>('/admin/employees', data);
    return res.data;
  },

  deleteEmployee: async (id: number): Promise<void> => {
    await api.delete(`/admin/employees/${id}`);
  },

  adminResetPassword: async (id: number, newPassword: string): Promise<void> => {
    await api.put(`/admin/employees/${id}/reset-password`, { newPassword });
  },
};
