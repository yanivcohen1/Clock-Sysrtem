import React from 'react';
import { AlertTriangle, X } from 'lucide-react';

interface Props {
  message: string;
  onDismiss?: () => void;
}

export const ErrorAlert: React.FC<Props> = ({ message, onDismiss }) => (
  <div className="rounded-lg border border-red-200 bg-red-50 p-4 flex items-start gap-3">
    <AlertTriangle className="h-5 w-5 text-red-500 mt-0.5 flex-shrink-0" />
    <p className="text-sm text-red-700 flex-1">{message}</p>
    {onDismiss && (
      <button onClick={onDismiss} className="text-red-400 hover:text-red-600">
        <X className="h-4 w-4" />
      </button>
    )}
  </div>
);
