import React from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Clock, LayoutDashboard, History, BarChart3, Users, Shield, LogOut, Menu, X, Lock } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';

interface Props {
  children: React.ReactNode;
}

export const Layout: React.FC<Props> = ({ children }) => {
  const { fullName, role, employeeCode, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [mobileOpen, setMobileOpen] = React.useState(false);

  const isActive = (path: string) =>
    location.pathname === path
      ? 'bg-primary-700 text-white'
      : 'text-primary-100 hover:bg-primary-600 hover:text-white';

  const navItems = [
    { path: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
    { path: '/attendance', label: 'Attendance', icon: Clock },
    { path: '/history', label: 'History', icon: History },
    ...(role === 'Manager' || role === 'Admin'
      ? [{ path: '/reports', label: 'Reports', icon: BarChart3 }]
      : []),
    ...(role === 'Admin'
      ? [{ path: '/admin', label: 'Admin', icon: Shield }]
      : []),
    { path: '/change-password', label: 'Change Password', icon: Lock },
  ];

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen flex">
      {/* Sidebar — desktop */}
      <aside className="hidden md:flex md:flex-col md:w-64 bg-primary-800 text-white">
        <div className="p-5 border-b border-primary-700">
          <h1 className="text-xl font-bold flex items-center gap-2">
            <Clock className="h-6 w-6" />
            HomeWorke
          </h1>
          <p className="text-primary-300 text-xs mt-1">Time & Attendance</p>
        </div>

        <nav className="flex-1 p-4 space-y-1">
          {navItems.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${isActive(item.path)}`}
            >
              <item.icon className="h-5 w-5" />
              {item.label}
            </Link>
          ))}
        </nav>

        <div className="p-4 border-t border-primary-700">
          <div className="text-sm">
            <p className="font-medium">{fullName}</p>
            <p className="text-primary-300 text-xs">{employeeCode} · {role}</p>
          </div>
          <button
            onClick={handleLogout}
            className="mt-3 flex items-center gap-2 text-primary-300 hover:text-white text-sm transition-colors"
          >
            <LogOut className="h-4 w-4" />
            Sign out
          </button>
        </div>
      </aside>

      {/* Mobile header */}
      <div className="flex-1 flex flex-col min-h-screen">
        <header className="md:hidden bg-primary-800 text-white px-4 py-3 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Clock className="h-5 w-5" />
            <span className="font-bold">HomeWorke</span>
          </div>
          <button onClick={() => setMobileOpen(!mobileOpen)}>
            {mobileOpen ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
          </button>
        </header>

        {/* Mobile nav */}
        {mobileOpen && (
          <nav className="md:hidden bg-primary-800 text-white p-4 space-y-1">
            {navItems.map((item) => (
              <Link
                key={item.path}
                to={item.path}
                onClick={() => setMobileOpen(false)}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium ${isActive(item.path)}`}
              >
                <item.icon className="h-5 w-5" />
                {item.label}
              </Link>
            ))}
            <button
              onClick={handleLogout}
              className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-primary-300 hover:text-white w-full"
            >
              <LogOut className="h-5 w-5" />
              Sign out
            </button>
          </nav>
        )}

        {/* Main content */}
        <main className="flex-1 p-4 md:p-8 bg-gray-50">{children}</main>
      </div>
    </div>
  );
};
