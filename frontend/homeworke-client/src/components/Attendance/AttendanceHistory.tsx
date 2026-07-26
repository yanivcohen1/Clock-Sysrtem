import React from 'react';
import type { AttendanceRecord } from '../../types';
import { StatusBadge } from '../Common/StatusBadge';
import { format } from 'date-fns';
import { Clock, Calendar, Timer } from 'lucide-react';

interface Props {
  records: AttendanceRecord[];
  loading: boolean;
}

export const AttendanceHistory: React.FC<Props> = ({ records, loading }) => {
  if (loading) {
    return (
      <div className="space-y-3 mt-4">
        {[1, 2, 3].map((i) => (
          <div key={i} className="bg-white rounded-lg border border-gray-200 p-4 animate-pulse">
            <div className="h-4 bg-gray-200 rounded w-1/3 mb-2" />
            <div className="h-3 bg-gray-100 rounded w-1/2" />
          </div>
        ))}
      </div>
    );
  }

  if (records.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500">
        <Calendar className="h-12 w-12 mx-auto mb-3 text-gray-300" />
        <p className="font-medium">No attendance records found</p>
        <p className="text-sm mt-1">Your clock in/out history will appear here</p>
      </div>
    );
  }

  return (
    <div className="space-y-3 mt-4">
      {records.map((record) => (
        <div
          key={record.id}
          className={`bg-white rounded-lg border p-4 transition-shadow hover:shadow-sm ${
            record.isOpen ? 'border-green-300 bg-green-50/50' : 'border-gray-200'
          }`}
        >
          <div className="flex items-center justify-between mb-2">
            <div className="flex items-center gap-2">
              <Calendar className="h-4 w-4 text-gray-400" />
              <span className="font-medium text-sm">
                {format(new Date(record.shiftDate), 'EEE, MMM dd, yyyy')}
              </span>
            </div>
            <StatusBadge status={record.isOpen ? 'Active' : record.status} />
          </div>

          <div className="grid grid-cols-2 gap-2 text-sm">
            <div className="flex items-center gap-1.5">
              <Clock className="h-3.5 w-3.5 text-green-500" />
              <span className="text-gray-500">In:</span>
              <span className="font-medium">{format(new Date(record.clockIn), 'HH:mm:ss')}</span>
            </div>
            <div className="flex items-center gap-1.5">
              <Clock className="h-3.5 w-3.5 text-red-500" />
              <span className="text-gray-500">Out:</span>
              <span className="font-medium">
                {record.clockOut ? format(new Date(record.clockOut), 'HH:mm:ss') : '—'}
              </span>
            </div>
          </div>

          {record.hoursWorked != null && (
            <div className="mt-2 flex items-center gap-1.5 text-sm">
              <Timer className="h-3.5 w-3.5 text-primary-500" />
              <span className="text-gray-500">Duration:</span>
              <span className="font-medium">{record.hoursWorked}h</span>
              {record.hoursWorked > 14 && (
                <span className="text-xs text-orange-600 ml-1">⚠ Long shift</span>
              )}
            </div>
          )}

          {record.notes && (
            <p className="mt-2 text-xs text-gray-400 italic">{record.notes}</p>
          )}

          {record.isManuallyAdjusted && (
            <span className="mt-2 inline-block text-xs bg-yellow-100 text-yellow-800 px-2 py-0.5 rounded">
              Adjusted by admin
            </span>
          )}
        </div>
      ))}
    </div>
  );
};
