import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import { loadIds, saveIds } from '@/lib/storedIds';

/** The last products opened in this browser, the latest first. */
export interface RecentlyViewedState {
  productIds: number[];
}

export const RECENTLY_VIEWED_KEY = 'recentlyViewed';
export const RECENTLY_VIEWED_MAX = 8;

export const loadRecentlyViewed = (): RecentlyViewedState => ({ productIds: loadIds(RECENTLY_VIEWED_KEY, RECENTLY_VIEWED_MAX) });
export const saveRecentlyViewed = (state: RecentlyViewedState) => saveIds(RECENTLY_VIEWED_KEY, state.productIds);

const recentlyViewedSlice = createSlice({
  name: 'recentlyViewed',
  initialState: (): RecentlyViewedState => ({ productIds: [] }),
  reducers: {
    /** A product page was opened: it moves to the front, the oldest falls off past the limit. */
    productViewed: (state, action: PayloadAction<number>) => {
      state.productIds = [action.payload, ...state.productIds.filter((id) => id !== action.payload)].slice(0, RECENTLY_VIEWED_MAX);
    },
  },
});

export const { productViewed } = recentlyViewedSlice.actions;
export default recentlyViewedSlice.reducer;
