import React, { useEffect, useState } from 'react';
import { attendanceService } from '../services/attendanceService';
import type { AttendanceRecord } from '../types';
import { AttendanceHistory } from '../components/Attendance/AttendanceHistory';
import { History, Filter } from 'lucide-react';
import { format, subMonths } from 'date-fns';

export const HistoryPage: React.FC = () => {
  const [records, setRecords] = useState<AttendanceRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [fromDate, setFromDate] = useState(format(subMonths(new Date(), 1), 'yyyy-MM-dd'));
  const [toDate, setToDate] = useState(format(new Date(), 'yyyy-MM-dd'));

  useEffect(() => {
    const fetchHistory = async () => {
      setLoading(true);
      try {
        const data = await attendanceService.getHistory(fromDate, toDate);
        setRecords(data);
      } catch {
        // Handled by axios interceptor
      } finally {
        setLoading(false);
      }
    };
    fetchHistory();
  }, [fromDate, toDate]);

  const totalHours = records.reduce((sum, r) => sum + (r.hoursWorked ?? 0), 0);

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
          <History className="h-6 w-6 text-primary-600" />
          Attendance History
        </h1>
        <p className="text-gray-500 text-sm mt-1">
          {records.length} records · {totalHours.toFixed(1)} total hours
        </p>
      </div>

      {/* Date filters */}
      <div className="bg-white rounded-xl border border-gray-200 p-4">
        <div className="flex items-center gap-2 mb-3">
          <Filter className="h-4 w-4 text-gray-400" />
          <span className="text-sm font-medium text-gray-700">Filter by date range</span>
        </div>
        <div className="flex gap-3">
          <div className="flex-1">
            <label className="block text-xs text-gray-500 mb-1">From</label>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none"
            />
          </div>
          <div className="flex-1">
            <label className="block text-xs text-gray-500 mb-1">To</label>
            <input
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none"
            />
          </div>
        </div>
      </div>

      <AttendanceHistory records={records} loading={loading} />
    </div>
  );
};
