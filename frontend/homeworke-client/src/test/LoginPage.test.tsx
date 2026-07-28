import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { LoginPage } from '../pages/LoginPage';
import { AuthProvider } from '../context/AuthContext';

const { mockLogin } = vi.hoisted(() => ({
  mockLogin: vi.fn(),
}));

vi.mock('../services/authService', () => ({
  authService: {
    login: mockLogin,
  },
}));

// Mock lucide-react to avoid "type is invalid" errors in jsdom
vi.mock('lucide-react', () => ({
  Clock: () => null,
  LogIn: () => null,
  Eye: () => null,
  EyeOff: () => null,
  UserPlus: () => null,
}));

function renderLoginPage() {
  return render(
    <MemoryRouter initialEntries={['/login']}>
      <AuthProvider>
        <LoginPage />
      </AuthProvider>
    </MemoryRouter>
  );
}

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('should render login form', () => {
    renderLoginPage();

    expect(screen.getByText(/sign in to your account/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText('you@company.com')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter your password')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('should render HomeWorke branding', () => {
    renderLoginPage();
    expect(screen.getByText('HomeWorke')).toBeInTheDocument();
    expect(screen.getByText(/Time & Attendance System/i)).toBeInTheDocument();
  });

  it('should call authService.login on form submission', async () => {
    mockLogin.mockResolvedValueOnce({
      token: 'test-jwt',
      fullName: 'John Doe',
      role: 'Employee',
      employeeCode: 'EMP-001',
    });

    renderLoginPage();

    await userEvent.type(screen.getByPlaceholderText('you@company.com'), 'john@test.com');
    await userEvent.type(screen.getByPlaceholderText('Enter your password'), 'Secure@123');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith({ email: 'john@test.com', password: 'Secure@123' });
    });
  });

  it('should show error on login failure', async () => {
    mockLogin.mockRejectedValueOnce({
      response: { data: { error: 'Invalid credentials' } },
    });

    renderLoginPage();

    await userEvent.type(screen.getByPlaceholderText('you@company.com'), 'bad@test.com');
    await userEvent.type(screen.getByPlaceholderText('Enter your password'), 'wrong');
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument();
    });
  });

  it('should have a link to forgot password page', () => {
    renderLoginPage();
    const forgotLink = screen.getByText(/forgot your password/i);
    expect(forgotLink).toBeInTheDocument();
    expect(forgotLink.closest('a')).toHaveAttribute('href', '/forgot-password');
  });

  it('should display demo credentials', () => {
    renderLoginPage();
    expect(screen.getByText(/admin@homeworke.com/)).toBeInTheDocument();
    expect(screen.getByText(/manager@homeworke.com/)).toBeInTheDocument();
    expect(screen.getByText(/demo@homeworke.com/)).toBeInTheDocument();
  });
});
