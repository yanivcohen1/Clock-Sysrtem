import { describe, it, expect, vi, beforeEach } from 'vitest';

const { mockedPost, mockedGet } = vi.hoisted(() => ({
  mockedPost: vi.fn(),
  mockedGet: vi.fn(),
}));

vi.mock('../services/api', () => ({
  default: {
    post: mockedPost,
    get: mockedGet,
  },
}));

import { attendanceService } from '../services/attendanceService';

describe('attendanceService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockRecord = {
    id: 1, employeeName: 'John Doe', employeeCode: 'EMP-001',
    shiftDate: '2026-07-28', clockIn: '2026-07-28T09:00:00', clockOut: null,
    hoursWorked: null, status: 'Present', isOpen: true,
    timeApiFailed: false, isManuallyAdjusted: false, notes: null,
  };

  it('should POST to /attendance/clock-in', async () => {
    mockedPost.mockResolvedValueOnce({ data: mockRecord });
    const result = await attendanceService.clockIn({ notes: 'Morning' });
    expect(mockedPost).toHaveBeenCalledWith('/attendance/clock-in', { notes: 'Morning' });
    expect(result).toEqual(mockRecord);
  });

  it('should clock in with empty request by default', async () => {
    mockedPost.mockResolvedValueOnce({ data: mockRecord });
    await attendanceService.clockIn();
    expect(mockedPost).toHaveBeenCalledWith('/attendance/clock-in', {});
  });

  it('should POST to /attendance/clock-out', async () => {
    const closedRecord = { ...mockRecord, clockOut: '2026-07-28T17:00:00', isOpen: false, hoursWorked: 8 };
    mockedPost.mockResolvedValueOnce({ data: closedRecord });
    const result = await attendanceService.clockOut({ notes: 'Done' });
    expect(mockedPost).toHaveBeenCalledWith('/attendance/clock-out', { notes: 'Done' });
    expect(result.isOpen).toBe(false);
  });

  it('should GET /attendance/status', async () => {
    mockedGet.mockResolvedValueOnce({ data: { isClockedIn: true, record: mockRecord } });
    const result = await attendanceService.getStatus();
    expect(mockedGet).toHaveBeenCalledWith('/attendance/status');
    expect(result.isClockedIn).toBe(true);
  });

  it('should GET /attendance/history with query params', async () => {
    mockedGet.mockResolvedValueOnce({ data: [mockRecord] });
    await attendanceService.getHistory('2026-07-01', '2026-07-31', 2, 20);
    const callUrl = mockedGet.mock.calls[0][0] as string;
    expect(callUrl).toContain('from=2026-07-01');
    expect(callUrl).toContain('to=2026-07-31');
    expect(callUrl).toContain('page=2');
    expect(callUrl).toContain('pageSize=20');
  });

  it('should get history with defaults', async () => {
    mockedGet.mockResolvedValueOnce({ data: [] });
    await attendanceService.getHistory();
    const callUrl = mockedGet.mock.calls[0][0] as string;
    expect(callUrl).toContain('page=1');
    expect(callUrl).toContain('pageSize=10');
  });

  it('should GET /reports/daily with date', async () => {
    mockedGet.mockResolvedValueOnce({
      data: { date: '2026-07-28', totalEmployees: 10, presentCount: 8, absentCount: 2, completedCount: 5, averageHours: 7.5, records: [] },
    });
    const result = await attendanceService.getDailyReport('2026-07-28');
    expect(mockedGet).toHaveBeenCalledWith('/reports/daily?date=2026-07-28&page=1&pageSize=10');
    expect(result.presentCount).toBe(8);
  });

  it('should GET /reports/monthly with year and month', async () => {
    mockedGet.mockResolvedValueOnce({ data: { data: [], totalCount: 0, page: 1, pageSize: 10 } });
    const result = await attendanceService.getMonthlyReport(2026, 7);
    expect(mockedGet).toHaveBeenCalledWith('/reports/monthly?year=2026&month=7&page=1&pageSize=10');
    expect(result.data).toEqual([]);
  });

  it('should GET /reports/current-status', async () => {
    mockedGet.mockResolvedValueOnce({ data: { employees: [], totalCount: 0, page: 1, pageSize: 10 } });
    const result = await attendanceService.getCurrentStatus(1, 20);
    expect(mockedGet).toHaveBeenCalledWith('/reports/current-status?page=1&pageSize=20');
    expect(result.employees).toEqual([]);
  });
});
