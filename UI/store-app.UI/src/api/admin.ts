import { api } from './api';
import type { AdminUsersResponse, UserResponse } from './types';

export type AdminUsersQuery = {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
};

/** User administration, served by the identity service; the demo administrator sees masked e-mails. */
export const adminApi = api.injectEndpoints({
  endpoints: (build) => ({
    getAdminUsers: build.query<AdminUsersResponse, AdminUsersQuery>({
      query: (params) => ({ url: '/admin/users', params }),
      providesTags: ['Users'],
    }),
    getAdminUser: build.query<UserResponse, string>({
      query: (id) => `/admin/users/${encodeURIComponent(id)}`,
      providesTags: (_result, _error, id) => [{ type: 'Users', id }],
    }),
  }),
});

export const { useGetAdminUsersQuery, useGetAdminUserQuery } = adminApi;
