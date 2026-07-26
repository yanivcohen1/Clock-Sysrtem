import React, { useEffect, useState, useCallback } from 'react';
import { attendanceService } from '../services/attendanceService';
import type { DailyReport, MonthlyReport, CurrentStatusResponse, PaginatedHistory, EmployeeDto } from '../types';
import { StatusBadge } from '../components/Common/StatusBadge';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { BarChart3, Calendar, Users, TrendingUp, Clock, History, Filter, ChevronLeft, ChevronRight, Search } from 'lucide-react';
import { format } from 'date-fns';

type ViewTab = 'daily' | 'monthly' | 'status' | 'history';

export const ReportsPage: React.FC = () => {
  const [view, setView] = useState<ViewTab>('status');
  const [date, setDate] = useState(format(new Date(), 'yyyy-MM-dd'));
  const [month, setMonth] = useState(new Date().getMonth() + 1);
  const [year, setYear] = useState(new Date().getFullYear());
  const [dailyReport, setDailyReport] = useState<DailyReport | null>(null);
  const [monthlyReport, setMonthlyReport] = useState<MonthlyReport[]>([]);
  const [currentStatus, setCurrentStatus] = useState<CurrentStatusResponse | null>(null);
  const [historyData, setHistoryData] = useState<PaginatedHistory | null>(null);
  const [historyPage, setHistoryPage] = useState(1);
  const [historyFrom, setHistoryFrom] = useState('');
  const [historyTo, setHistoryTo] = useState('');
  const [historyEmployeeId, setHistoryEmployeeId] = useState<number | undefined>();
  const [employees, setEmployees] = useState<EmployeeDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const fetchDaily = useCallback(async () => {
    setLoading(true); setError('');
    try { const data = await attendanceService.getDailyReport(date); setDailyReport(data); }
    catch (err: any) { setError(err.response?.data?.error || 'Failed to load report'); }
    finally { setLoading(false); }
  }, [date]);

  const fetchMonthly = useCallback(async () => {
    setLoading(true); setError('');
    try { const data = await attendanceService.getMonthlyReport(year, month); setMonthlyReport(data); }
    catch (err: any) { setError(err.response?.data?.error || 'Failed to load report'); }
    finally { setLoading(false); }
  }, [year, month]);

  const fetchCurrentStatus = useCallback(async () => {
    setLoading(true); setError('');
    try { const data = await attendanceService.getCurrentStatus(); setCurrentStatus(data); }
    catch (err: any) { setError(err.response?.data?.error || 'Failed to load status'); }
    finally { setLoading(false); }
  }, []);

  const fetchHistory = useCallback(async (page: number) => {
    setLoading(true); setError('');
    try {
      const data = await attendanceService.getReportHistory({
        employeeId: historyEmployeeId,
        from: historyFrom || undefined,
        to: historyTo || undefined,
        page,
        pageSize: 20,
      });
      setHistoryData(data);
    } catch (err: any) { setError(err.response?.data?.error || 'Failed to load history'); }
    finally { setLoading(false); }
  }, [historyEmployeeId, historyFrom, historyTo]);

  const fetchEmployees = useCallback(async () => {
    try { const data = await attendanceService.getEmployees(); setEmployees(data); }
    catch { /* non-critical */ }
  }, []);

  useEffect(() => {
    if (view === 'daily') fetchDaily();
    else if (view === 'monthly') fetchMonthly();
    else if (view === 'status') fetchCurrentStatus();
    else if (view === 'history') { fetchHistory(historyPage); fetchEmployees(); }
  }, [view, date, month, year]);

  useEffect(() => {
    if (view === 'history') fetchHistory(historyPage);
  }, [historyPage, historyEmployeeId, historyFrom, historyTo]);

  const tabs: { key: ViewTab; label: string; icon: React.FC<{ className?: string }> }[] = [
    { key: 'status', label: 'Current Status', icon: Clock },
    { key: 'daily', label: 'Daily Report', icon: Calendar },
    { key: 'monthly', label: 'Monthly Report', icon: BarChart3 },
    { key: 'history', label: 'History', icon: History },
  ];

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
          <BarChart3 className="h-6 w-6 text-primary-600" />
          Reports
        </h1>
        <p className="text-gray-500 text-sm mt-1">Your attendance reports & team overview</p>
      </div>

      {/* View tabs */}
      <div className="flex gap-1 bg-gray-100 rounded-lg p-1 overflow-x-auto">
        {tabs.map(({ key, label, icon: Icon }) => (
          <button
            key={key}
            onClick={() => { setView(key); setHistoryPage(1); }}
            className={`flex items-center gap-1.5 px-4 py-2 rounded-md text-sm font-medium transition-colors whitespace-nowrap ${
              view === key
                ? 'bg-white text-primary-700 shadow-sm'
                : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
            }`}
          >
            <Icon className="h-4 w-4" />
            {label}
          </button>
        ))}
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-700">{error}</div>
      )}

      {loading && <LoadingSpinner text="Loading..." />}

      {/* ─── CURRENT STATUS ─── */}
      {!loading && view === 'status' && currentStatus && (
        <div className="space-y-6">
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <StatCard icon={Users} label="Total Employees" value={currentStatus.totalEmployees} color="blue" />
            <StatCard icon={TrendingUp} label="Working Now" value={currentStatus.workingNow} color="green" />
            <StatCard icon={Calendar} label="Not Working" value={currentStatus.notWorking} color="red" />
          </div>

          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-6 py-4 bg-gray-50 border-b border-gray-200">
              <h3 className="font-semibold text-gray-900">All Employees — Right Now</h3>
              <p className="text-sm text-gray-500">{currentStatus.workingNow} of {currentStatus.totalEmployees} currently clocked in</p>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Employee</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Department</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Status</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Clock In</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Hours Today</th>
                  </tr>
                </thead>
                <tbody>
                  {currentStatus.employees.map((e, i) => (
                    <tr key={i} className="border-t border-gray-100 hover:bg-gray-50">
                      <td className="px-4 py-2.5">
                        <span className="font-medium">{e.employeeName}</span>
                        <span className="text-gray-400 ml-1 text-xs">({e.employeeCode})</span>
                      </td>
                      <td className="px-4 py-2.5 text-gray-600">{e.department}</td>
                      <td className="px-4 py-2.5">
                        {e.isWorking ? (
                          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
                            <span className="w-1.5 h-1.5 rounded-full bg-green-500 animate-pulse" />
                            Working
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-500">
                            Not Working
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-2.5">
                        {e.clockIn ? format(new Date(e.clockIn), 'HH:mm:ss') : '—'}
                      </td>
                      <td className="px-4 py-2.5 font-medium">
                        {e.hoursWorkedToday != null ? `${e.hoursWorkedToday.toFixed(1)}h` : '—'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* ─── DAILY REPORT ─── */}
      {!loading && view === 'daily' && (
        <div className="space-y-6">
          <div className="bg-white rounded-xl border border-gray-200 p-4">
            <label className="block text-xs text-gray-500 mb-1">Select Date</label>
            <input
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none"
            />
          </div>

          {dailyReport && (
            <>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <StatCard icon={Users} label="Total Employees" value={dailyReport.totalEmployees} color="blue" />
                <StatCard icon={TrendingUp} label="Working Now" value={dailyReport.presentCount} color="green" />
                <StatCard icon={Calendar} label="Not Clocked In" value={dailyReport.absentCount} color="red" />
                <StatCard icon={TrendingUp} label="Completed Shifts" value={dailyReport.completedCount} color="yellow" />
              </div>

              <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
                <div className="px-6 py-4 bg-gray-50 border-b border-gray-200">
                  <h3 className="font-semibold text-gray-900">
                    {format(new Date(dailyReport.date), 'EEEE, MMMM dd, yyyy')}
                  </h3>
                  <p className="text-sm text-gray-500">
                    Average: {dailyReport.averageHours}h · {dailyReport.records.length} records
                  </p>
                </div>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="text-left px-4 py-2 font-medium text-gray-600">Employee</th>
                        <th className="text-left px-4 py-2 font-medium text-gray-600">Clock In</th>
                        <th className="text-left px-4 py-2 font-medium text-gray-600">Clock Out</th>
                        <th className="text-left px-4 py-2 font-medium text-gray-600">Hours</th>
                        <th className="text-left px-4 py-2 font-medium text-gray-600">Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {dailyReport.records.map((r, i) => (
                        <tr key={i} className="border-t border-gray-100 hover:bg-gray-50">
                          <td className="px-4 py-2.5">
                            <span className="font-medium">{r.employeeName}</span>
                            <span className="text-gray-400 ml-1 text-xs">({r.employeeCode})</span>
                          </td>
                          <td className="px-4 py-2.5">{r.clockIn ? format(new Date(r.clockIn), 'HH:mm:ss') : '—'}</td>
                          <td className="px-4 py-2.5">{r.clockOut ? format(new Date(r.clockOut), 'HH:mm:ss') : '—'}</td>
                          <td className="px-4 py-2.5">{r.hoursWorked?.toFixed(1) ?? '—'}h</td>
                          <td className="px-4 py-2.5"><StatusBadge status={r.status} /></td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </>
          )}
        </div>
      )}

      {/* ─── MONTHLY REPORT ─── */}
      {!loading && view === 'monthly' && (
        <div className="space-y-6">
          <div className="bg-white rounded-xl border border-gray-200 p-4 flex flex-wrap gap-4 items-end">
            <div>
              <label className="block text-xs text-gray-500 mb-1">Year</label>
              <input
                type="number" value={year} onChange={(e) => setYear(Number(e.target.value))}
                min={2000} max={2100}
                className="w-24 rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none"
              />
            </div>
            <div>
              <label className="block text-xs text-gray-500 mb-1">Month</label>
              <select
                value={month} onChange={(e) => setMonth(Number(e.target.value))}
                className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none"
              >
                {Array.from({ length: 12 }, (_, i) => (
                  <option key={i + 1} value={i + 1}>
                    {format(new Date(2024, i, 1), 'MMMM')}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-6 py-4 bg-gray-50 border-b border-gray-200">
              <h3 className="font-semibold text-gray-900">
                {format(new Date(year, month - 1, 1), 'MMMM yyyy')}
              </h3>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Employee</th>
                    <th className="text-center px-4 py-2 font-medium text-gray-600">Days Worked</th>
                    <th className="text-center px-4 py-2 font-medium text-gray-600">Absent</th>
                    <th className="text-center px-4 py-2 font-medium text-gray-600">Late</th>
                    <th className="text-center px-4 py-2 font-medium text-gray-600">Total Hours</th>
                    <th className="text-center px-4 py-2 font-medium text-gray-600">Avg/Day</th>
                  </tr>
                </thead>
                <tbody>
                  {monthlyReport.map((r, i) => (
                    <tr key={i} className="border-t border-gray-100 hover:bg-gray-50">
                      <td className="px-4 py-2.5 font-medium">{r.employeeName}</td>
                      <td className="px-4 py-2.5 text-center">{r.daysWorked}</td>
                      <td className="px-4 py-2.5 text-center text-red-600">{r.daysAbsent}</td>
                      <td className="px-4 py-2.5 text-center text-yellow-600">{r.daysLate}</td>
                      <td className="px-4 py-2.5 text-center font-medium">{r.totalHours}h</td>
                      <td className="px-4 py-2.5 text-center">{r.averageDailyHours}h</td>
                    </tr>
                  ))}
                  {monthlyReport.length === 0 && (
                    <tr>
                      <td colSpan={6} className="px-4 py-8 text-center text-gray-400">
                        No data for this month
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* ─── HISTORY ─── */}
      {!loading && view === 'history' && (
        <div className="space-y-6">
          {/* Filters */}
          <div className="bg-white rounded-xl border border-gray-200 p-4 space-y-3">
            <div className="flex items-center gap-2 text-sm font-medium text-gray-700">
              <Filter className="h-4 w-4" />
              Filters
            </div>
            <div className="flex flex-wrap gap-3 items-end">
              <div>
                <label className="block text-xs text-gray-500 mb-1">From Date</label>
                <input
                  type="date" value={historyFrom}
                  onChange={(e) => { setHistoryFrom(e.target.value); setHistoryPage(1); }}
                  className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none"
                />
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">To Date</label>
                <input
                  type="date" value={historyTo}
                  onChange={(e) => { setHistoryTo(e.target.value); setHistoryPage(1); }}
                  className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none"
                />
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Employee</label>
                <select
                  value={historyEmployeeId ?? ''}
                  onChange={(e) => { setHistoryEmployeeId(e.target.value ? Number(e.target.value) : undefined); setHistoryPage(1); }}
                  className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none min-w-[180px]"
                >
                  <option value="">All Employees</option>
                  {employees.map((emp) => (
                    <option key={emp.id} value={emp.id}>{emp.fullName}</option>
                  ))}
                </select>
              </div>
              <button
                onClick={() => { setHistoryFrom(''); setHistoryTo(''); setHistoryEmployeeId(undefined); setHistoryPage(1); }}
                className="px-3 py-2 text-sm text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
              >
                Clear Filters
              </button>
            </div>
          </div>

          {/* History Table */}
          {historyData && (
            <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
              <div className="px-6 py-4 bg-gray-50 border-b border-gray-200 flex justify-between items-center">
                <div>
                  <h3 className="font-semibold text-gray-900">Attendance History</h3>
                  <p className="text-sm text-gray-500">{historyData.totalCount} total records</p>
                </div>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Employee</th>
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Date</th>
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Clock In</th>
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Clock Out</th>
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Hours</th>
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {historyData.records.map((r) => (
                      <tr key={r.id} className="border-t border-gray-100 hover:bg-gray-50">
                        <td className="px-4 py-2.5">
                          <span className="font-medium">{r.employeeName}</span>
                          <span className="text-gray-400 ml-1 text-xs">({r.employeeCode})</span>
                        </td>
                        <td className="px-4 py-2.5">{format(new Date(r.shiftDate), 'MMM dd, yyyy')}</td>
                        <td className="px-4 py-2.5">{format(new Date(r.clockIn), 'HH:mm:ss')}</td>
                        <td className="px-4 py-2.5">{r.clockOut ? format(new Date(r.clockOut), 'HH:mm:ss') : '—'}</td>
                        <td className="px-4 py-2.5">{r.hoursWorked?.toFixed(1) ?? '—'}h</td>
                        <td className="px-4 py-2.5"><StatusBadge status={r.status} /></td>
                      </tr>
                    ))}
                    {historyData.records.length === 0 && (
                      <tr>
                        <td colSpan={6} className="px-4 py-8 text-center text-gray-400">
                          No records found
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>

              {/* Pagination */}
              {historyData.totalCount > historyData.pageSize && (
                <div className="px-6 py-3 bg-gray-50 border-t border-gray-200 flex items-center justify-between">
                  <span className="text-sm text-gray-500">
                    Page {historyData.page} of {Math.ceil(historyData.totalCount / historyData.pageSize)}
                  </span>
                  <div className="flex gap-2">
                    <button
                      onClick={() => setHistoryPage((p) => Math.max(1, p - 1))}
                      disabled={historyData.page <= 1}
                      className="flex items-center gap-1 px-3 py-1.5 text-sm rounded-md border border-gray-300 bg-white hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                      <ChevronLeft className="h-4 w-4" /> Prev
                    </button>
                    <button
                      onClick={() => setHistoryPage((p) => p + 1)}
                      disabled={historyData.page * historyData.pageSize >= historyData.totalCount}
                      className="flex items-center gap-1 px-3 py-1.5 text-sm rounded-md border border-gray-300 bg-white hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                      Next <ChevronRight className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
};

const StatCard: React.FC<{
  icon: React.FC<{ className?: string }>;
  label: string;
  value: number;
  color: 'blue' | 'green' | 'red' | 'yellow';
}> = ({ icon: Icon, label, value, color }) => {
  const colors = {
    blue: 'bg-blue-100 text-blue-600',
    green: 'bg-green-100 text-green-600',
    red: 'bg-red-100 text-red-600',
    yellow: 'bg-yellow-100 text-yellow-600',
  };
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-4 flex items-center gap-3">
      <div className={`h-10 w-10 rounded-lg flex items-center justify-center ${colors[color]}`}>
        <Icon className="h-5 w-5" />
      </div>
      <div>
        <p className="text-sm text-gray-500">{label}</p>
        <p className="text-xl font-bold text-gray-900">{value}</p>
      </div>
    </div>
  );
};
