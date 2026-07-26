import { useState, useCallback } from 'react';
import { attendanceService } from '../services/attendanceService';
import type { AttendanceRecord, AttendanceStatus } from '../types';

export function useAttendance() {
  const [status, setStatus] = useState<AttendanceStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchStatus = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await attendanceService.getStatus();
      setStatus(result);
    } catch (err: any) {
      setError(err.response?.data?.detail || err.response?.data?.error || 'Failed to fetch status');
    } finally {
      setLoading(false);
    }
  }, []);

  const clockIn = useCallback(async (notes?: string): Promise<AttendanceRecord | null> => {
    try {
      setLoading(true);
      setError(null);
      const result = await attendanceService.clockIn({ notes });
      setStatus({ isClockedIn: true, record: result });
      return result;
    } catch (err: any) {
      const msg = err.response?.data?.detail || err.response?.data?.error || 'Clock-in failed';
      setError(msg);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  const clockOut = useCallback(async (notes?: string): Promise<AttendanceRecord | null> => {
    try {
      setLoading(true);
      setError(null);
      const result = await attendanceService.clockOut({ notes });
      setStatus({ isClockedIn: false });
      return result;
    } catch (err: any) {
      const msg = err.response?.data?.detail || err.response?.data?.error || 'Clock-out failed';
      setError(msg);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  return { status, loading, error, fetchStatus, clockIn, clockOut, setError };
}
