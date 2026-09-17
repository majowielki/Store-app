import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { UserResponse } from '@/api/types';

export interface SessionState {
  /** The profile from /auth/me (or the sign-in response); null for a visitor. */
  user: UserResponse | null;
  /** True once the session was restored or refused after the page load; guards wait for it. */
  checked: boolean;
}

const initialState: SessionState = { user: null, checked: false };

// Nothing here talks to the API: src/features/session/sessionThunks.ts signs in, restores
// and signs out, and reports the outcome with these actions. The access token itself is
// kept in src/api/session.ts, never in the store or in storage.
const sessionSlice = createSlice({
  name: 'session',
  initialState,
  reducers: {
    /** A session is in place and the guest cart, if any, has been merged. */
    sessionStarted: (state, action: PayloadAction<UserResponse>) => {
      state.user = action.payload;
      state.checked = true;
    },
    /** Signed out, or the refresh token was refused. */
    sessionEnded: (state) => {
      state.user = null;
      state.checked = true;
    },
    /** The profile as /auth/me answered after the page load, or nothing for a visitor. */
    profileLoaded: (state, action: PayloadAction<UserResponse | null>) => {
      state.user = action.payload;
      state.checked = true;
    },
    /**
     * The checkout asked for the delivery address to be kept; the identity service stores it
     * a moment later (through an event), so the profile shown here is updated at once.
     */
    addressSaved: (state, action: PayloadAction<string>) => {
      if (state.user) state.user.simpleAddress = action.payload;
    },
  },
});

export const { sessionStarted, sessionEnded, profileLoaded, addressSaved } = sessionSlice.actions;

export default sessionSlice.reducer;
