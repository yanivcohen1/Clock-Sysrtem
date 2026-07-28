import { describe, it, expect, afterEach } from 'vitest';

describe('API Service', () => {
  afterEach(() => {
    localStorage.clear();
  });

  it('should export an API instance with /api base URL', async () => {
    const api = (await import('../services/api')).default;
    expect(api).toBeDefined();
    expect(typeof api.get).toBe('function');
    expect(typeof api.post).toBe('function');
    expect(api.defaults.baseURL).toBe('/api');
  });

  it('should register request and response interceptors', async () => {
    const api = (await import('../services/api')).default;
    expect(api.interceptors.request).toBeDefined();
    expect(api.interceptors.response).toBeDefined();
  });
});
