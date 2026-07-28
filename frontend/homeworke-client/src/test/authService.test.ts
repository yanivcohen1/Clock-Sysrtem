import { describe, it, expect, vi, beforeEach } from 'vitest';

const { mockedPost } = vi.hoisted(() => ({
  mockedPost: vi.fn(),
}));

vi.mock('../services/api', () => ({
  default: {
    post: mockedPost,
    get: vi.fn(),
  },
}));

import { authService } from '../services/authService';

describe('authService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should call POST /auth/login with credentials', async () => {
    mockedPost.mockResolvedValueOnce({
      data: { token: 'jwt-token', fullName: 'John Doe', role: 'Employee' as const, employeeCode: 'EMP-001' },
    });

    const result = await authService.login({ email: 'john@test.com', password: 'Secure@123' });

    expect(mockedPost).toHaveBeenCalledWith('/auth/login', { email: 'john@test.com', password: 'Secure@123' });
    expect(result.token).toBe('jwt-token');
  });

  it('should propagate login errors', async () => {
    mockedPost.mockRejectedValueOnce(new Error('Invalid credentials'));

    await expect(authService.login({ email: 'bad@test.com', password: 'wrong' })).rejects.toThrow('Invalid credentials');
  });

  it('should call POST /auth/register with user data', async () => {
    mockedPost.mockResolvedValueOnce({
      data: { token: 'jwt-new', fullName: 'Jane Doe', role: 'Employee' as const, employeeCode: 'EMP-002' },
    });

    const result = await authService.register({ firstName: 'Jane', lastName: 'Doe', email: 'jane@test.com', password: 'Secure@123', departmentId: 1 });

    expect(mockedPost).toHaveBeenCalledWith('/auth/register', { firstName: 'Jane', lastName: 'Doe', email: 'jane@test.com', password: 'Secure@123', departmentId: 1 });
    expect(result.token).toBe('jwt-new');
  });

  it('should call POST /auth/change-password', async () => {
    mockedPost.mockResolvedValueOnce({ data: {} });

    await authService.changePassword({ currentPassword: 'OldPass@123', newPassword: 'NewPass@456' });

    expect(mockedPost).toHaveBeenCalledWith('/auth/change-password', { currentPassword: 'OldPass@123', newPassword: 'NewPass@456' });
  });

  it('should call POST /auth/forgot-password', async () => {
    mockedPost.mockResolvedValueOnce({ data: { message: 'Reset email sent', resetToken: null } });

    const result = await authService.forgotPassword('user@test.com');

    expect(mockedPost).toHaveBeenCalledWith('/auth/forgot-password', { email: 'user@test.com' });
    expect(result.message).toBe('Reset email sent');
  });

  it('should call POST /auth/reset-password', async () => {
    mockedPost.mockResolvedValueOnce({ data: {} });

    await authService.resetPassword('user@test.com', 'token-abc', 'NewPass@789');

    expect(mockedPost).toHaveBeenCalledWith('/auth/reset-password', { email: 'user@test.com', resetToken: 'token-abc', newPassword: 'NewPass@789' });
  });
});
