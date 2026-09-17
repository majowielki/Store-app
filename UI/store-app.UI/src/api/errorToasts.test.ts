import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { authApi } from './auth';
import { catalogApi } from './catalog';
import { toast } from '@/hooks/use-toast';
import { createAppStore } from '@/store';
import { api, problemResponse } from '@/test/handlers';
import { server } from '@/test/server';
import { signIn } from '@/features/session/sessionThunks';
import { vi } from 'vitest';

vi.mock('@/hooks/use-toast', () => ({ toast: vi.fn() }));

describe('error toasts', () => {
  it('shows the problem detail once for a failed request', async () => {
    server.use(http.get(api('/products/1'), () => problemResponse(404, 'Product 1 was not found')));
    const store = createAppStore();

    await store.dispatch(catalogApi.endpoints.getProduct.initiate(1));

    expect(toast).toHaveBeenCalledTimes(1);
    expect(toast).toHaveBeenCalledWith({ description: 'Product 1 was not found', variant: 'destructive' });
  });

  it('shows one toast for a refused sign-in, not one per action', async () => {
    server.use(http.post(api('/auth/login'), () => problemResponse(401, 'Invalid email or password')));
    const store = createAppStore();

    await store.dispatch(signIn({ kind: 'login', credentials: { email: 'x@y.z', password: 'nope' } }));

    expect(toast).toHaveBeenCalledTimes(1);
    expect(toast).toHaveBeenCalledWith({ description: 'Invalid email or password', variant: 'destructive' });
  });

  it('keeps quiet about endpoints marked silent', async () => {
    server.use(http.get(api('/auth/me'), () => problemResponse(401, 'no')));
    const store = createAppStore();

    await store.dispatch(authApi.endpoints.getMe.initiate());

    expect(toast).not.toHaveBeenCalled();
  });

  it('keeps quiet about the 401 that ended the session', async () => {
    server.use(http.get(api('/products/1'), () => problemResponse(401, 'expired')));
    const store = createAppStore();
    const { setAccessToken } = await import('./session');
    setAccessToken('stale');

    await store.dispatch(catalogApi.endpoints.getProduct.initiate(1));

    expect(toast).not.toHaveBeenCalled();
  });
});
