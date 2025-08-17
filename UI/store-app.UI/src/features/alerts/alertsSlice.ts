import { createSlice, PayloadAction } from '@reduxjs/toolkit';

export type AlertKind = 'default' | 'destructive';
export interface AppAlert {
  id: string;
  title?: string;
  description?: string;
  variant?: AlertKind;
}

interface AlertsState {
  items: AppAlert[];
}

const initialState: AlertsState = {
  items: [],
};

const alertsSlice = createSlice({
  name: 'alerts',
  initialState,
  reducers: {
    addAlert: (state, action: PayloadAction<AppAlert>) => {
      state.items.push(action.payload);
    },
    removeAlert: (state, action: PayloadAction<string>) => {
      state.items = state.items.filter((a) => a.id !== action.payload);
    },
    clearAlerts: (state) => {
      state.items = [];
    },
  },
});

export const { addAlert, removeAlert, clearAlerts } = alertsSlice.actions;
export default alertsSlice.reducer;
