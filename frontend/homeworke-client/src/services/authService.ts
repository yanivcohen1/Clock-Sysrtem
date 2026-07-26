import api from './api';
import type { LoginRequest, LoginResponse, RegisterRequest, ChangePasswordRequest } from '../types';

export const authService = {
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const res = await api.post<LoginResponse>('/auth/login', data);
    return res.data;
  },

  register: async (data: RegisterRequest): Promise<LoginResponse> => {
    const res = await api.post<LoginResponse>('/auth/register', data);
    return res.data;
  },

  changePassword: async (data: ChangePasswordRequest): Promise<void> => {
    await api.post('/auth/change-password', data);
  },

  forgotPassword: async (email: string): Promise<{ message: string; resetToken: string | null }> => {
    const res = await api.post('/auth/forgot-password', { email });
    return res.data;
  },

  resetPassword: async (email: string, resetToken: string, newPassword: string): Promise<void> => {
    await api.post('/auth/reset-password', { email, resetToken, newPassword });
  },
};
