import { api } from './api';
import type { AuthResponse, LoginRequest, RegisterRequest, UserResponse } from './types';

// The session itself (access token in memory, refresh cookie in the browser) is handled by
// src/api/session.ts and baseQuery.ts; these endpoints only carry the requests. Signing in
// and out is orchestrated in src/features/user (merge of the guest cart, cache reset).
export const authApi = api.injectEndpoints({
  endpoints: (build) => ({
    login: build.mutation<AuthResponse, LoginRequest>({
      query: (credentials) => ({ url: '/auth/login', method: 'POST', body: credentials }),
    }),
    register: build.mutation<AuthResponse, RegisterRequest>({
      query: (body) => ({ url: '/auth/register', method: 'POST', body }),
    }),
    demoLogin: build.mutation<AuthResponse, void>({
      query: () => ({ url: '/auth/demo-login', method: 'POST' }),
    }),
    demoAdminLogin: build.mutation<AuthResponse, void>({
      query: () => ({ url: '/auth/demo-admin-login', method: 'POST' }),
    }),
    /** Ends the session on the server: the refresh cookie is revoked and removed. */
    logout: build.mutation<void, void>({
      query: () => ({ url: '/auth/logout', method: 'POST' }),
    }),
    /** The signed-in user's profile; undefined for an anonymous caller (204). */
    getMe: build.query<UserResponse | undefined, void>({
      query: () => '/auth/me',
      extraOptions: { silent: true },
    }),
  }),
});

// Sign-in and sign-out go through src/features/session/sessionThunks.ts, so no hooks are exported here
