import React, { useState, useEffect } from 'react';
import { LogIn, LogOut, Clock, AlertCircle } from 'lucide-react';
import { useAttendance } from '../../hooks/useAttendance';
import { LoadingSpinner } from '../Common/LoadingSpinner';
import { ErrorAlert } from '../Common/ErrorAlert';
import { format } from 'date-fns';

export const ClockButton: React.FC = () => {
  const { status, loading, error, fetchStatus, clockIn, clockOut, setError } = useAttendance();
  const [notes, setNotes] = useState('');
  const [lastAction, setLastAction] = useState<string | null>(null);

  // Fetch status on mount
  useEffect(() => {
    fetchStatus();
  }, [fetchStatus]);

  const handleClockIn = async () => {
    const result = await clockIn(notes || undefined);
    if (result) {
      setLastAction(`Clocked in at ${format(new Date(result.clockIn), 'HH:mm:ss')} (Zurich time)`);
      setNotes('');
    }
  };

  const handleClockOut = async () => {
    const result = await clockOut(notes || undefined);
    if (result) {
      setLastAction(
        `Clocked out at ${format(new Date(result.clockOut!), 'HH:mm:ss')} · ` +
        `Shift: ${result.hoursWorked}h`
      );
      setNotes('');
    }
  };

  const isClockedIn = status?.isClockedIn ?? false;
  const currentRecord = status?.record;

  return (
    <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6 md:p-8">
      {/* Status indicator */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">
            {isClockedIn ? 'Currently Working' : 'Ready to Start'}
          </h2>
          <p className="text-sm text-gray-500 mt-1">
            All times are recorded in Europe/Zurich timezone via external API
          </p>
        </div>
        <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-sm font-medium ${
          isClockedIn
            ? 'bg-green-100 text-green-700'
            : 'bg-gray-100 text-gray-600'
        }`}>
          <span className={`h-2 w-2 rounded-full ${isClockedIn ? 'bg-green-500 animate-pulse' : 'bg-gray-400'}`} />
          {isClockedIn ? 'Clocked In' : 'Clocked Out'}
        </div>
      </div>

      {/* Current shift info */}
      {isClockedIn && currentRecord && (
        <div className="bg-blue-50 rounded-lg p-4 mb-6">
          <div className="flex items-center gap-2 text-blue-800 text-sm font-medium mb-2">
            <Clock className="h-4 w-4" />
            Current Shift
          </div>
          <div className="grid grid-cols-2 gap-3 text-sm">
            <div>
              <span className="text-gray-500">Clock In:</span>{' '}
              <span className="font-medium">
                {format(new Date(currentRecord.clockIn), 'HH:mm:ss')}
              </span>
            </div>
            <div>
              <span className="text-gray-500">Date:</span>{' '}
              <span className="font-medium">
                {format(new Date(currentRecord.shiftDate), 'MMM dd, yyyy')}
              </span>
            </div>
          </div>
        </div>
      )}

      {/* Notes field */}
      <div className="mb-6">
        <label htmlFor="notes" className="block text-sm font-medium text-gray-700 mb-1">
          Notes (optional)
        </label>
        <input
          id="notes"
          type="text"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder={isClockedIn ? 'e.g., Leaving early for doctor appointment' : 'e.g., Arrived via train delay'}
          className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none"
        />
      </div>

      {/* Error */}
      {error && (
        <div className="mb-4">
          <ErrorAlert message={error} onDismiss={() => setError(null)} />
        </div>
      )}

      {/* Success message */}
      {lastAction && !error && (
        <div className="mb-4 rounded-lg bg-green-50 border border-green-200 p-3 flex items-center gap-2 text-sm text-green-700">
          <AlertCircle className="h-4 w-4" />
          {lastAction}
        </div>
      )}

      {/* Clock buttons */}
      <div className="flex gap-4">
        {!isClockedIn ? (
          <button
            onClick={handleClockIn}
            disabled={loading}
            className="flex-1 flex items-center justify-center gap-2 bg-green-600 hover:bg-green-700 disabled:bg-green-400 text-white font-semibold py-3 px-6 rounded-xl transition-colors text-lg"
          >
            {loading ? (
              <LoadingSpinner text="" />
            ) : (
              <>
                <LogIn className="h-6 w-6" />
                Clock In
              </>
            )}
          </button>
        ) : (
          <button
            onClick={handleClockOut}
            disabled={loading}
            className="flex-1 flex items-center justify-center gap-2 bg-red-600 hover:bg-red-700 disabled:bg-red-400 text-white font-semibold py-3 px-6 rounded-xl transition-colors text-lg"
          >
            {loading ? (
              <LoadingSpinner text="" />
            ) : (
              <>
                <LogOut className="h-6 w-6" />
                Clock Out
              </>
            )}
          </button>
        )}
      </div>

      {/* Timezone disclaimer */}
      <p className="mt-4 text-xs text-gray-400 text-center">
        ⏱ All timestamps sourced from WorldTimeAPI.org (Europe/Zurich)
      </p>
    </div>
  );
};
