import React, { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { attendanceService } from '../services/attendanceService';
import type { AttendanceRecord } from '../types';
import { Clock, Timer, CalendarCheck, AlertTriangle } from 'lucide-react';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { format } from 'date-fns';

export const DashboardPage: React.FC = () => {
  const { fullName, employeeCode } = useAuth();
  const [status, setStatus] = useState<{ isClockedIn: boolean; record?: AttendanceRecord } | null>(null);
  const [todayRecords, setTodayRecords] = useState<AttendanceRecord[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [s, history] = await Promise.all([
          attendanceService.getStatus(),
          attendanceService.getHistory(),
        ]);
        setStatus(s);
        // Filter today's records
        const today = new Date().toISOString().split('T')[0];
        setTodayRecords(history.filter((r) => r.shiftDate === today));
      } catch {
        // Handle gracefully
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  if (loading) return <LoadingSpinner text="Loading dashboard..." />;

  const currentRecord = status?.record;
  const todayHours = todayRecords.reduce((sum, r) => sum + (r.hoursWorked ?? 0), 0);

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Welcome */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Welcome back, {fullName}</h1>
        <p className="text-gray-500 text-sm mt-1">
          {employeeCode} · {format(new Date(), 'EEEE, MMMM dd, yyyy')}
        </p>
      </div>

      {/* Stats cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3">
            <div className={`h-10 w-10 rounded-lg flex items-center justify-center ${
              status?.isClockedIn ? 'bg-green-100' : 'bg-gray-100'
            }`}>
              <Clock className={`h-5 w-5 ${status?.isClockedIn ? 'text-green-600' : 'text-gray-400'}`} />
            </div>
            <div>
              <p className="text-sm text-gray-500">Status</p>
              <p className={`font-semibold ${status?.isClockedIn ? 'text-green-600' : 'text-gray-600'}`}>
                {status?.isClockedIn ? 'Clocked In' : 'Clocked Out'}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3">
            <div className="h-10 w-10 rounded-lg bg-blue-100 flex items-center justify-center">
              <Timer className="h-5 w-5 text-blue-600" />
            </div>
            <div>
              <p className="text-sm text-gray-500">Today's Hours</p>
              <p className="font-semibold text-gray-900">{todayHours.toFixed(1)}h</p>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3">
            <div className="h-10 w-10 rounded-lg bg-purple-100 flex items-center justify-center">
              <CalendarCheck className="h-5 w-5 text-purple-600" />
            </div>
            <div>
              <p className="text-sm text-gray-500">Today's Entries</p>
              <p className="font-semibold text-gray-900">{todayRecords.length}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Active shift alert */}
      {status?.isClockedIn && currentRecord && (
        <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 flex items-start gap-3">
          <AlertTriangle className="h-5 w-5 text-blue-600 mt-0.5" />
          <div>
            <p className="font-medium text-blue-800">Active Shift in Progress</p>
            <p className="text-sm text-blue-600 mt-1">
              Clocked in at {format(new Date(currentRecord.clockIn), 'HH:mm:ss')} (Zurich time).
              Don't forget to clock out when your shift ends!
            </p>
          </div>
        </div>
      )}

      {/* Quick tip */}
      <div className="bg-gradient-to-r from-primary-50 to-blue-50 rounded-xl border border-primary-100 p-5">
        <h3 className="font-semibold text-primary-800 mb-2">💡 Did you know?</h3>
        <p className="text-sm text-primary-600">
          All attendance times are verified against an external time API for the Europe/Zurich timezone.
          This ensures accuracy across all locations and prevents time manipulation.
        </p>
      </div>
    </div>
  );
};
