import React from 'react';
import { ClockButton } from '../components/Attendance/ClockButton';
import { Clock } from 'lucide-react';

export const AttendancePage: React.FC = () => {
  return (
    <div className="max-w-lg mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
          <Clock className="h-6 w-6 text-primary-600" />
          Attendance
        </h1>
        <p className="text-gray-500 text-sm mt-1">
          Clock in when you start your shift. Clock out when you finish.
        </p>
      </div>

      <ClockButton />

      {/* Info cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <h3 className="font-medium text-gray-900 text-sm">🔒 Double Clock-In Prevention</h3>
          <p className="text-xs text-gray-500 mt-1">
            You cannot clock in again if you have an active shift. You must clock out first.
          </p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <h3 className="font-medium text-gray-900 text-sm">🌐 Time Source</h3>
          <p className="text-xs text-gray-500 mt-1">
            All times are sourced from WorldTimeAPI.org for the Europe/Zurich timezone — never from your browser.
          </p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <h3 className="font-medium text-gray-900 text-sm">🚩 Auto-Flagging</h3>
          <p className="text-xs text-gray-500 mt-1">
            Shifts longer than 14 hours are automatically flagged for admin review.
          </p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <h3 className="font-medium text-gray-900 text-sm">📝 Full Audit Trail</h3>
          <p className="text-xs text-gray-500 mt-1">
            Every clock-in, clock-out, and adjustment is permanently logged for transparency.
          </p>
        </div>
      </div>
    </div>
  );
};
