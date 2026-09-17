import { createSlice, createAsyncThunk, type PayloadAction } from "@reduxjs/toolkit";
import type { UserState, LoginRequest, RegisterRequest, UserResponse, AuthResponse } from "@/utils/types";
import { authApi } from "@/utils/api";
import { refreshAccessToken } from "@/utils/customFetch";
import { getErrorMessage } from "@/utils/errorHandling";
import { setAccessToken } from "@/utils/session";
import { toast } from "@/hooks/use-toast";

// The profile is cached so the header shows the name at once after a reload; the access token
// is never stored - the refresh cookie brings a new one (see utils/session.ts)
const USER_CACHE_KEY = 'authUser';

const readCachedUser = (): UserResponse | null => {
  try {
    const raw = localStorage.getItem(USER_CACHE_KEY);
    return raw ? (JSON.parse(raw) as UserResponse) : null;
  } catch {
    return null;
  }
};

const cacheUser = (user: UserResponse | null) => {
  try {
    if (user) localStorage.setItem(USER_CACHE_KEY, JSON.stringify(user));
    else localStorage.removeItem(USER_CACHE_KEY);
  } catch {
    // ignore storage errors
  }
};

const signedIn = (state: UserState, session: AuthResponse) => {
  state.user = session.user;
  state.error = null;
  state.sessionChecked = true;
  setAccessToken(session.accessToken);
  cacheUser(session.user);
};

const signedOut = (state: UserState) => {
  state.user = null;
  state.error = null;
  state.sessionChecked = true;
  setAccessToken(null);
  cacheUser(null);
};

// Async thunks
export const loginUserAsync = createAsyncThunk(
  'user/login',
  async (credentials: LoginRequest, { rejectWithValue }) => {
    try {
      return await authApi.login(credentials);
    } catch (error: unknown) {
      return rejectWithValue(getErrorMessage(error) || 'Login failed');
    }
  }
);

export const registerUserAsync = createAsyncThunk(
  'user/register',
  async (userData: RegisterRequest, { rejectWithValue }) => {
    try {
      return await authApi.register(userData);
    } catch (error: unknown) {
      return rejectWithValue(getErrorMessage(error) || 'Registration failed');
    }
  }
);

export const logoutUserAsync = createAsyncThunk(
  'user/logout',
  async (_, { rejectWithValue }) => {
    try {
      await authApi.logout();
      return null;
    } catch (error: unknown) {
      // The session on this device ends either way; the server side is revoked next time
      return rejectWithValue(getErrorMessage(error) || 'Logout failed');
    }
  }
);

/**
 * Continues the session after a page load: the refresh cookie is traded for an access token,
 * then the profile is read. Without a cookie the visitor is anonymous, which is not an error.
 */
export const restoreSessionAsync = createAsyncThunk(
  'user/restoreSession',
  async (_, { rejectWithValue }) => {
    const token = await refreshAccessToken();
    if (!token) return rejectWithValue(null);
    try {
      return await authApi.getCurrentUser();
    } catch (error: unknown) {
      return rejectWithValue(getErrorMessage(error) || 'Failed to get user info');
    }
  }
);

export const getCurrentUserAsync = createAsyncThunk(
  'user/getCurrentUser',
  async (_, { rejectWithValue }) => {
    try {
      return await authApi.getCurrentUser();
    } catch (error: unknown) {
      return rejectWithValue(getErrorMessage(error) || 'Failed to get user info');
    }
  }
);

const getInitialState = (): UserState => ({
  user: readCachedUser(),
  isLoading: false,
  error: null,
  sessionChecked: false,
});

const userSlice = createSlice({
  name: 'user',
  initialState: getInitialState(),
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    /** Forgets the user without calling the API - the session ended on its own (refresh refused). */
    clearUser: (state) => {
      signedOut(state);
    },
    /** A session established outside the thunks (demo logins). */
    sessionStarted: (state, action: PayloadAction<AuthResponse>) => {
      signedIn(state, action.payload);
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(loginUserAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(loginUserAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        signedIn(state, action.payload);
        toast({ description: 'Successfully logged in!' });
      })
      .addCase(loginUserAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
        toast({
          description: action.payload as string || 'Login failed',
          variant: 'destructive'
        });
      });

    builder
      .addCase(registerUserAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(registerUserAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        signedIn(state, action.payload);
        toast({ description: 'Successfully registered!' });
      })
      .addCase(registerUserAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
        toast({
          description: action.payload as string || 'Registration failed',
          variant: 'destructive'
        });
      });

    builder
      .addCase(logoutUserAsync.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(logoutUserAsync.fulfilled, (state) => {
        state.isLoading = false;
        signedOut(state);
      })
      .addCase(logoutUserAsync.rejected, (state, action) => {
        state.isLoading = false;
        signedOut(state);
        state.error = action.payload as string;
      });

    builder
      .addCase(restoreSessionAsync.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(restoreSessionAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        if (action.payload) {
          state.user = action.payload;
          state.sessionChecked = true;
          cacheUser(action.payload);
        } else {
          signedOut(state);
        }
      })
      .addCase(restoreSessionAsync.rejected, (state) => {
        state.isLoading = false;
        signedOut(state);
      });

    builder
      .addCase(getCurrentUserAsync.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(getCurrentUserAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.error = null;
        state.user = action.payload;
        cacheUser(action.payload);
      })
      .addCase(getCurrentUserAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });
  },
});

export const { clearError, clearUser, sessionStarted } = userSlice.actions;

export default userSlice.reducer;
