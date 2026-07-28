import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useAttendance } from '../hooks/useAttendance';

const { mockGetStatus, mockClockIn, mockClockOut } = vi.hoisted(() => ({
  mockGetStatus: vi.fn(),
  mockClockIn: vi.fn(),
  mockClockOut: vi.fn(),
}));

vi.mock('../services/attendanceService', () => ({
  attendanceService: {
    getStatus: mockGetStatus,
    clockIn: mockClockIn,
    clockOut: mockClockOut,
    getHistory: vi.fn(),
  },
}));

describe('useAttendance', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockRecord = {
    id: 1, employeeName: 'John Doe', employeeCode: 'EMP-001',
    shiftDate: '2026-07-28', clockIn: '2026-07-28T09:00:00', clockOut: null,
    hoursWorked: null, status: 'Present', isOpen: true,
    timeApiFailed: false, isManuallyAdjusted: false, notes: null,
  };

  it('should start with default state', () => {
    const { result } = renderHook(() => useAttendance());
    expect(result.current.status).toBeNull();
    expect(result.current.loading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('should fetch status and update state', async () => {
    mockGetStatus.mockResolvedValueOnce({ isClockedIn: true, record: mockRecord });
    const { result } = renderHook(() => useAttendance());

    await act(async () => {
      await result.current.fetchStatus();
    });

    expect(result.current.status).toEqual({ isClockedIn: true, record: mockRecord });
    expect(result.current.loading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('should handle fetch status error', async () => {
    mockGetStatus.mockRejectedValueOnce({ response: { data: { detail: 'Service unavailable' } } });
    const { result } = renderHook(() => useAttendance());

    await act(async () => {
      await result.current.fetchStatus();
    });

    expect(result.current.error).toBe('Service unavailable');
  });

  it('should clock in and update status', async () => {
    mockClockIn.mockResolvedValueOnce(mockRecord);
    const { result } = renderHook(() => useAttendance());

    let clockInResult: unknown;
    await act(async () => {
      clockInResult = await result.current.clockIn('Morning shift');
    });

    expect(mockClockIn).toHaveBeenCalledWith({ notes: 'Morning shift' });
    expect(result.current.status).toEqual({ isClockedIn: true, record: mockRecord });
    expect(clockInResult).toEqual(mockRecord);
  });

  it('should handle clock-in error', async () => {
    mockClockIn.mockRejectedValueOnce({ response: { data: { error: 'Already clocked in' } } });
    const { result } = renderHook(() => useAttendance());

    let clockInResult: unknown;
    await act(async () => {
      clockInResult = await result.current.clockIn();
    });

    expect(clockInResult).toBeNull();
    expect(result.current.error).toBe('Already clocked in');
  });

  it('should clock out and update status', async () => {
    const closedRecord = { ...mockRecord, clockOut: '2026-07-28T17:00:00', isOpen: false, hoursWorked: 8 };
    mockClockOut.mockResolvedValueOnce(closedRecord);
    const { result } = renderHook(() => useAttendance());

    let clockOutResult: unknown;
    await act(async () => {
      clockOutResult = await result.current.clockOut('Done');
    });

    expect(mockClockOut).toHaveBeenCalledWith({ notes: 'Done' });
    expect(result.current.status).toEqual({ isClockedIn: false });
    expect(clockOutResult).toEqual(closedRecord);
  });
});
