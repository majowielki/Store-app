import { createAsyncThunk } from '@reduxjs/toolkit';
import { api } from '@/api/api';
import { authApi } from '@/api/auth';
import { refreshAccessToken } from '@/api/baseQuery';
import { cartApi } from '@/api/cart';
import { hasRememberedSession, rememberSession, setAccessToken } from '@/api/session';
import type { AuthResponse, LoginRequest, RegisterRequest, UserResponse } from '@/api/types';
import type { ApiError } from '@/api/problem';
import type { AppDispatch, RootState } from '@/store';
import { cleared } from '@/features/cart/guestCartSlice';
import { profileLoaded, sessionEnded, sessionStarted } from './sessionSlice';

type ThunkConfig = { state: RootState; dispatch: AppDispatch; rejectValue: ApiError };

// Nobody reads these mutations back through a hook, so their results need not be kept in the cache
const untracked = { track: false } as const;

export type SignInRequest =
  | { kind: 'login'; credentials: LoginRequest }
  | { kind: 'register'; details: RegisterRequest }
  | { kind: 'demoUser' }
  | { kind: 'demoAdmin' };

const startSession = (request: SignInRequest, dispatch: AppDispatch): Promise<AuthResponse> => {
  switch (request.kind) {
    case 'login':
      return dispatch(authApi.endpoints.login.initiate(request.credentials, untracked)).unwrap();
    case 'register':
      return dispatch(authApi.endpoints.register.initiate(request.details, untracked)).unwrap();
    case 'demoUser':
      return dispatch(authApi.endpoints.demoLogin.initiate(undefined, untracked)).unwrap();
    case 'demoAdmin':
      return dispatch(authApi.endpoints.demoAdminLogin.initiate(undefined, untracked)).unwrap();
  }
};

/**
 * The cart a visitor filled before signing in joins the server cart once; the local copy is
 * then dropped, so nothing is merged twice. A failure here leaves the guest cart in place for
 * the next sign-in and does not stop the session from starting.
 */
const mergeGuestCart = async (dispatch: AppDispatch, getState: () => RootState): Promise<void> => {
  const items = getState().guestCart.items;
  if (items.length === 0) return;
  try {
    await dispatch(
      cartApi.endpoints.syncCart.initiate(
        { items: items.map((item) => ({ productId: item.productId, quantity: item.quantity, color: item.color })) },
        untracked,
      ),
    ).unwrap();
    dispatch(cleared());
  } catch {
    // Already reported by the error middleware
  }
};

/**
 * Signs in (any of the four ways), merges the guest cart and only then tells the app the
 * session is on - so the cart the pages read next is the merged one. The rejection carries the
 * ApiError of the sign-in request; the toast for it has been shown by then.
 */
export const signIn = createAsyncThunk<UserResponse, SignInRequest, ThunkConfig>(
  'session/signIn',
  async (request, { dispatch, getState, rejectWithValue }) => {
    let session: AuthResponse;
    try {
      session = await startSession(request, dispatch);
    } catch (error) {
      return rejectWithValue(error as ApiError);
    }
    setAccessToken(session.accessToken);
    rememberSession(true);
    // Whatever another account left in the cache must not be shown to this one
    dispatch(api.util.invalidateTags(['Cart', 'Orders', 'Users', 'Stats']));
    await mergeGuestCart(dispatch, getState);
    dispatch(sessionStarted(session.user));
    return session.user;
  },
);

/**
 * Once per page load: the refresh cookie is traded for an access token and the profile is
 * read. Without a cookie the visitor is anonymous, which is not an error. Either way the
 * session counts as checked afterwards, which is what the route guards wait for.
 */
export const restoreSession = createAsyncThunk<UserResponse | null, void, ThunkConfig>(
  'session/restore',
  async (_, { dispatch, getState }) => {
    // A visitor who never signed in here has no refresh cookie to trade; skip the request
    const token = hasRememberedSession() ? await refreshAccessToken() : null;
    if (!token) {
      dispatch(sessionEnded());
      return null;
    }
    const request = dispatch(authApi.endpoints.getMe.initiate(undefined, { forceRefetch: true }));
    const user = (await request).data ?? null;
    request.unsubscribe();
    // An account that no longer exists (404) has no session either
    if (!user) setAccessToken(null);
    else await mergeGuestCart(dispatch, getState);
    dispatch(profileLoaded(user));
    return user;
  },
);

/**
 * Ends the session on the server (best effort - this device forgets it either way) and drops
 * everything the cache holds for the account. The server cart belongs to the account and is
 * there on the next sign-in.
 */
export const signOut = createAsyncThunk<void, void, ThunkConfig>('session/signOut', async (_, { dispatch }) => {
  try {
    await dispatch(authApi.endpoints.logout.initiate(undefined, untracked)).unwrap();
  } catch {
    // Reported by the error middleware; the refresh token is revoked by the server on its next use
  }
  setAccessToken(null);
  rememberSession(false);
  dispatch(sessionEnded());
  dispatch(api.util.resetApiState());
});
