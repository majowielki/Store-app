import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { cartApi } from '@/api/cart';
import { getAccessToken, hasRememberedSession, rememberSession } from '@/api/session';
import type { SyncCartRequest } from '@/api/types';
import { createAppStore } from '@/store';
import { admin, cartWithTable, session, user } from '@/test/fixtures';
import { api, json, problemResponse } from '@/test/handlers';
import { server } from '@/test/server';
import { itemAdded } from '@/features/cart/guestCartSlice';
import { isAdmin } from './roles';
import { restoreSession, signIn, signOut } from './sessionThunks';

const table = { productId: 7, title: 'Oak Table', image: 'img', company: 'luxora', color: 'brown', unitPrice: 320, quantity: 2 };

describe('signing in', () => {
  it('keeps the token, merges the guest cart once and only then reports the session', async () => {
    const synced: SyncCartRequest[] = [];
    server.use(
      http.post(api('/cart/sync'), async ({ request }) => {
        synced.push((await request.json()) as SyncCartRequest);
        return json(cartWithTable(2));
      }),
    );
    const store = createAppStore();
    store.dispatch(itemAdded(table));

    const result = await store.dispatch(signIn({ kind: 'login', credentials: { email: user.email, password: 'secret-1!' } })).unwrap();

    expect(result).toEqual(user);
    expect(getAccessToken()).toBe(`token-for-${user.id}`);
    expect(hasRememberedSession()).toBe(true);
    expect(synced).toEqual([{ items: [{ productId: 7, quantity: 2, color: 'brown' }] }]);
    expect(store.getState().guestCart.items).toEqual([]);
    expect(store.getState().session).toEqual({ user, checked: true });
    // The merged cart is what the pages read next
    expect(cartApi.endpoints.getCart.select()(store.getState()).data?.totalItems).toBe(2);
  });

  it('does not call the server when the guest cart is empty', async () => {
    let syncCalls = 0;
    server.use(
      http.post(api('/cart/sync'), () => {
        syncCalls += 1;
        return json(cartWithTable());
      }),
    );
    const store = createAppStore();

    await store.dispatch(signIn({ kind: 'demoUser' })).unwrap();

    expect(syncCalls).toBe(0);
    expect(store.getState().session.user).toEqual(user);
  });

  it('rejects with the problem of a refused login and leaves the visitor anonymous', async () => {
    server.use(http.post(api('/auth/login'), () => problemResponse(401, 'Invalid email or password')));
    const store = createAppStore();

    await expect(store.dispatch(signIn({ kind: 'login', credentials: { email: 'x@y.z', password: 'nope' } })).unwrap()).rejects.toMatchObject({
      status: 401,
      message: 'Invalid email or password',
    });
    expect(getAccessToken()).toBeNull();
    expect(store.getState().session.user).toBeNull();
  });

  it('recognises the administrator roles from the profile', async () => {
    server.use(http.post(api('/auth/login'), () => json(session(admin))));
    const store = createAppStore();

    const result = await store.dispatch(signIn({ kind: 'login', credentials: { email: admin.email, password: 'secret-1!' } })).unwrap();

    expect(isAdmin(result)).toBe(true);
    expect(isAdmin(user)).toBe(false);
    expect(isAdmin({ roles: ['demo-admin'] })).toBe(true);
    expect(isAdmin(null)).toBe(false);
  });
});

describe('restoring the session after a page load', () => {
  it('settles as anonymous without a request when nobody signed in here', async () => {
    let refreshCalls = 0;
    server.use(
      http.post(api('/auth/refresh'), () => {
        refreshCalls += 1;
        return json(session());
      }),
    );
    const store = createAppStore();

    const result = await store.dispatch(restoreSession()).unwrap();

    expect(result).toBeNull();
    expect(refreshCalls).toBe(0);
    expect(store.getState().session).toEqual({ user: null, checked: true });
  });

  it('settles as anonymous when the refresh cookie is refused, and stops asking', async () => {
    rememberSession(true);
    const store = createAppStore();

    const result = await store.dispatch(restoreSession()).unwrap();

    expect(result).toBeNull();
    expect(hasRememberedSession()).toBe(false);
    expect(store.getState().session).toEqual({ user: null, checked: true });
  });

  it('trades the cookie for a token and reads the profile', async () => {
    server.use(http.post(api('/auth/refresh'), () => json(session())));
    rememberSession(true);
    const store = createAppStore();

    const result = await store.dispatch(restoreSession()).unwrap();

    expect(result).toEqual(user);
    expect(getAccessToken()).toBe(`token-for-${user.id}`);
    expect(store.getState().session).toEqual({ user, checked: true });
  });
});

describe('signing out', () => {
  it('forgets the token, the user and everything cached for the account', async () => {
    const store = createAppStore();
    await store.dispatch(signIn({ kind: 'demoUser' })).unwrap();
    await store.dispatch(cartApi.endpoints.getCart.initiate());
    expect(cartApi.endpoints.getCart.select()(store.getState()).data).toBeDefined();

    await store.dispatch(signOut()).unwrap();

    expect(getAccessToken()).toBeNull();
    expect(hasRememberedSession()).toBe(false);
    expect(store.getState().session).toEqual({ user: null, checked: true });
    expect(cartApi.endpoints.getCart.select()(store.getState()).data).toBeUndefined();
  });

  it('ends the session on this device even when the server refuses', async () => {
    server.use(http.post(api('/auth/logout'), () => problemResponse(500, 'boom')));
    const store = createAppStore();
    await store.dispatch(signIn({ kind: 'demoUser' })).unwrap();

    await store.dispatch(signOut()).unwrap();

    expect(getAccessToken()).toBeNull();
    expect(store.getState().session.user).toBeNull();
  });
});
