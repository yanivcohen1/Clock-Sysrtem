import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';
import type { LoginResponse, UserRole } from '../types';
import { authService } from '../services/authService';

interface AuthState {
  isAuthenticated: boolean;
  token: string | null;
  fullName: string;
  role: UserRole;
  employeeCode: string;
}

interface AuthContextType extends AuthState {
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [auth, setAuth] = useState<AuthState>(() => {
    const token = localStorage.getItem('token');
    const userStr = localStorage.getItem('user');
    if (token && userStr) {
      try {
        const user = JSON.parse(userStr) as LoginResponse;
        return {
          isAuthenticated: true,
          token,
          fullName: user.fullName,
          role: user.role,
          employeeCode: user.employeeCode,
        };
      } catch {
        // Corrupted data — clear
        localStorage.removeItem('token');
        localStorage.removeItem('user');
      }
    }
    return {
      isAuthenticated: false,
      token: null,
      fullName: '',
      role: 'Employee',
      employeeCode: '',
    };
  });

  const login = useCallback(async (email: string, password: string) => {
    const result = await authService.login({ email, password });
    localStorage.setItem('token', result.token);
    localStorage.setItem('user', JSON.stringify(result));
    setAuth({
      isAuthenticated: true,
      token: result.token,
      fullName: result.fullName,
      role: result.role,
      employeeCode: result.employeeCode,
    });
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setAuth({
      isAuthenticated: false,
      token: null,
      fullName: '',
      role: 'Employee',
      employeeCode: '',
    });
  }, []);

  return (
    <AuthContext.Provider value={{ ...auth, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
