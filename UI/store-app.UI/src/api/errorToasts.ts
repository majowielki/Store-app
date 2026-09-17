import { isRejectedWithValue, type Middleware } from '@reduxjs/toolkit';
import { toast } from '@/hooks/use-toast';
import { api } from './api';
import type { ApiResultMeta } from './baseQuery';
import { isApiError } from './problem';

const isApiAction = (type: string) => type.startsWith(`${api.reducerPath}/`);

/**
 * The one place a failed request turns into a toast: every rejected endpoint, once, with the
 * message the problem response carries. Silent: endpoints marked so (a missing filter list
 * or an anonymous /auth/me are not the user's concern) and the 401 that ended the session,
 * which the session listener reports in its own words.
 */
export const errorToasts: Middleware = () => (next) => (action) => {
  if (isRejectedWithValue(action) && isApiAction(action.type) && isApiError(action.payload)) {
    const meta = action.meta as { baseQueryMeta?: ApiResultMeta };
    const silent = meta.baseQueryMeta?.silent === true || action.payload.sessionEnded === true;
    if (!silent) {
      toast({ description: action.payload.message, variant: 'destructive' });
    }
  }
  return next(action);
};
