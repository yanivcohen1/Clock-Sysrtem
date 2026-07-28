import axios from 'axios';

// ── Loading-bar event bus ──────────────────────────
const emitLoadingStart = () => window.dispatchEvent(new CustomEvent('loading-bar:start'));
const emitLoadingComplete = () => window.dispatchEvent(new CustomEvent('loading-bar:complete'));

// ── Notistack error event bus ──────────────────────
export const emitNotistackError = (message: string) =>
  window.dispatchEvent(new CustomEvent('notistack:error', { detail: { message } }));

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

// Track active requests so the bar stays until ALL finish
let activeRequests = 0;

// ── Request interceptor: attach JWT token + start loading bar ──
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  activeRequests++;
  emitLoadingStart();
  return config;
});

// ── Response interceptor: stop loading bar, handle errors ────
api.interceptors.response.use(
  (response) => {
    activeRequests--;
    if (activeRequests <= 0) {
      activeRequests = 0;
      emitLoadingComplete();
    }
    return response;
  },
  (error) => {
    activeRequests--;
    if (activeRequests <= 0) {
      activeRequests = 0;
      emitLoadingComplete();
    }

    // 401 — force logout
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
      return Promise.reject(error);
    }

    // 403 — forbidden
    if (error.response?.status === 403) {
      emitNotistackError('Access denied. You do not have permission.');
      return Promise.reject(error);
    }

    // Network error (server unreachable, no internet, timeout)
    if (!error.response) {
      emitNotistackError('Unable to connect to the server. Please check your connection.');
      return Promise.reject(error);
    }

    // 5xx server errors
    if (error.response.status >= 500) {
      emitNotistackError('Server error. Please try again later.');
      return Promise.reject(error);
    }

    // 4xx client errors (except 401/403) — pass through, let pages handle inline
    return Promise.reject(error);
  }
);

export default api;
