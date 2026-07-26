import React from 'react';

interface Props {
  status: string;
}

const statusStyles: Record<string, string> = {
  Present: 'bg-green-100 text-green-800',
  Absent: 'bg-red-100 text-red-800',
  Late: 'bg-yellow-100 text-yellow-800',
  EarlyDeparture: 'bg-orange-100 text-orange-800',
  HalfDay: 'bg-blue-100 text-blue-800',
  OnLeave: 'bg-purple-100 text-purple-800',
  Holiday: 'bg-pink-100 text-pink-800',
};

export const StatusBadge: React.FC<Props> = ({ status }) => {
  const style = statusStyles[status] || 'bg-gray-100 text-gray-800';
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${style}`}>
      {status}
    </span>
  );
};
