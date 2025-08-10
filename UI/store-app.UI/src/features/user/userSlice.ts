import { createSlice, createAsyncThunk, type PayloadAction } from "@reduxjs/toolkit";
import type { UserState, LoginRequest, RegisterRequest, UserResponse } from "@/utils/types";
import { authApi } from "@/utils/api";
import { getErrorMessage } from "@/utils/errorHandling";
import { toast } from "@/hooks/use-toast";

// Async thunks
export const loginUserAsync = createAsyncThunk(
  'user/login',
  async (credentials: LoginRequest, { rejectWithValue }) => {
    try {
      const response = await authApi.login(credentials);
      if (response.success && response.accessToken) {
        // Store token in localStorage
        localStorage.setItem('authToken', response.accessToken);
        return response;
      } else {
        return rejectWithValue(response.message || 'Login failed');
      }
    } catch (error: unknown) {
      const message = getErrorMessage(error) || 'Login failed';
      return rejectWithValue(message);
    }
  }
);

export const registerUserAsync = createAsyncThunk(
  'user/register',
  async (userData: RegisterRequest, { rejectWithValue }) => {
    try {
      const response = await authApi.register(userData);
      if (response.success && response.accessToken) {
        // Store token in localStorage
        localStorage.setItem('authToken', response.accessToken);
        return response;
      } else {
        return rejectWithValue(response.message || 'Registration failed');
      }
    } catch (error: unknown) {
      const message = getErrorMessage(error) || 'Registration failed';
      return rejectWithValue(message);
    }
  }
);

export const logoutUserAsync = createAsyncThunk(
  'user/logout',
  async (_, { rejectWithValue }) => {
    try {
      await authApi.logout();
      // Clear token from localStorage
      localStorage.removeItem('authToken');
      return null;
    } catch (error: unknown) {
      // Even if API call fails, we should clear local storage
      localStorage.removeItem('authToken');
      const message = getErrorMessage(error) || 'Logout failed';
      return rejectWithValue(message);
    }
  }
);

export const getCurrentUserAsync = createAsyncThunk(
  'user/getCurrentUser',
  async (_, { rejectWithValue }) => {
    try {
      const user = await authApi.getCurrentUser();
      return user;
    } catch (error: unknown) {
      const message = getErrorMessage(error) || 'Failed to get user info';
      return rejectWithValue(message);
    }
  }
);

// Helper function to get initial state
const getInitialState = (): UserState => {
  const token = localStorage.getItem('authToken');
  return {
    user: null,
    token,
    isLoading: false,
    error: null,
    meAttempted: false,
  };
};

const userSlice = createSlice({
  name: 'user',
  initialState: getInitialState(),
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    clearUser: (state) => {
      state.user = null;
      state.token = null;
      state.error = null;
  state.meAttempted = true;
      localStorage.removeItem('authToken');
    },
    setUser: (state, action: PayloadAction<UserResponse>) => {
      state.user = action.payload;
    },
    setToken: (state, action: PayloadAction<string>) => {
      state.token = action.payload;
      localStorage.setItem('authToken', action.payload);
    },
    // Legacy action for backward compatibility
    loginUser: (state, action: PayloadAction<{ username: string; jwt: string }>) => {
      const { username, jwt } = action.payload;
      state.token = jwt;
      state.user = {
        id: 'demo',
        email: 'demo@example.com',
        userName: username,
        displayName: username,
        roles: [],
        isActive: true,
        createdAt: new Date().toISOString(),
      };
      localStorage.setItem('authToken', jwt);
      if (username === "demo user") {
        toast({ description: "Welcome Guest User" });
        return;
      }
      toast({ description: "Login successful" });
    },
    logoutUser: (state) => {
      state.user = null;
      state.token = null;
      state.error = null;
      localStorage.removeItem('authToken');
    },
  },
  extraReducers: (builder) => {
    // Login
    builder
      .addCase(loginUserAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(loginUserAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.error = null;
        if (action.payload.user && action.payload.accessToken) {
          state.user = action.payload.user;
          state.token = action.payload.accessToken;
        }
        toast({ description: 'Successfully logged in!' });
      })
      .addCase(loginUserAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
        toast({ 
          description: action.payload as string || 'Login failed',
          variant: 'destructive'
        });
      })
      
    // Register
    builder
      .addCase(registerUserAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(registerUserAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.error = null;
        if (action.payload.user && action.payload.accessToken) {
          state.user = action.payload.user;
          state.token = action.payload.accessToken;
        }
        toast({ description: 'Successfully registered!' });
      })
      .addCase(registerUserAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
        toast({ 
          description: action.payload as string || 'Registration failed',
          variant: 'destructive'
        });
      })
      
    // Logout
    builder
      .addCase(logoutUserAsync.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(logoutUserAsync.fulfilled, (state) => {
        state.isLoading = false;
        state.user = null;
        state.token = null;
        state.error = null;
        toast({ description: 'Successfully logged out!' });
      })
      .addCase(logoutUserAsync.rejected, (state, action) => {
        state.isLoading = false;
        // Still clear user data even if logout API call fails
        state.user = null;
        state.token = null;
        state.error = action.payload as string;
      })
      
    // Get Current User
    builder
      .addCase(getCurrentUserAsync.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(getCurrentUserAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.user = action.payload;
        state.error = null;
    state.meAttempted = true;
      })
      .addCase(getCurrentUserAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
    state.meAttempted = true;
    // Do not eagerly clear token here; interceptor handles expiry/invalid.
      });
  },
});

export const { clearError, clearUser, setUser, setToken, loginUser, logoutUser } = userSlice.actions;

export default userSlice.reducer;
