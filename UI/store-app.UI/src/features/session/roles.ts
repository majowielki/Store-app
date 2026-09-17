import type { UserResponse } from '@/api/types';

// The role names the identity service issues (Store.Contracts.Authorization.Roles); the
// profile from /auth/me carries them and nothing in the UI guesses a role from an e-mail.
export const Roles = {
  TrueAdmin: 'true-admin',
  DemoAdmin: 'demo-admin',
  User: 'user',
} as const;

/** May open the admin panel: the true administrator and the read-only demo one. */
export const isAdmin = (user: Pick<UserResponse, 'roles'> | null | undefined): boolean =>
  !!user && user.roles.some((role) => role === Roles.TrueAdmin || role === Roles.DemoAdmin);
