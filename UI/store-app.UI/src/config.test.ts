import { afterEach, describe, expect, it, vi } from 'vitest';

afterEach(() => {
  delete window.__APP_CONFIG__;
  vi.resetModules();
});

describe('runtime settings', () => {
  it('uses defaults when the container provides no operational settings', async () => {
    const config = await import('./config');
    expect(config.httpOptions).toEqual({ requestTimeoutMs: 15_000, renewBeforeExpiryMs: 30_000 });
    expect(config.dashboardDays).toBe(30);
  });

  it('accepts bounded runtime settings without rebuilding the application', async () => {
    window.__APP_CONFIG__ = { requestTimeoutMs: 120_000, renewBeforeExpiryMs: 0, dashboardDays: 3 };
    const config = await import('./config');
    expect(config.httpOptions).toEqual({ requestTimeoutMs: 120_000, renewBeforeExpiryMs: 0 });
    expect(config.dashboardDays).toBe(3);
  });

  it('rejects settings that would remove the timeout or request an invalid date range', async () => {
    window.__APP_CONFIG__ = { requestTimeoutMs: Infinity, renewBeforeExpiryMs: -1, dashboardDays: 1.5 };
    const config = await import('./config');
    expect(config.httpOptions).toEqual({ requestTimeoutMs: 15_000, renewBeforeExpiryMs: 30_000 });
    expect(config.dashboardDays).toBe(30);
  });
});
