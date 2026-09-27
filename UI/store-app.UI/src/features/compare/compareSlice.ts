import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import { loadIds, saveIds } from '@/lib/storedIds';

/** The products picked for comparison, in the order they were picked. */
export interface CompareState {
  productIds: number[];
}

export const COMPARE_KEY = 'compare';
export const COMPARE_MAX = 4;

export const loadCompare = (): CompareState => ({ productIds: loadIds(COMPARE_KEY, COMPARE_MAX) });
export const saveCompare = (state: CompareState) => saveIds(COMPARE_KEY, state.productIds);

const compareSlice = createSlice({
  name: 'compare',
  initialState: (): CompareState => ({ productIds: [] }),
  reducers: {
    /** Adds a product; a full list stays as it is (the button says why first). */
    addedToCompare: (state, action: PayloadAction<number>) => {
      if (!state.productIds.includes(action.payload) && state.productIds.length < COMPARE_MAX) state.productIds.push(action.payload);
    },
    removedFromCompare: (state, action: PayloadAction<number>) => {
      state.productIds = state.productIds.filter((id) => id !== action.payload);
    },
    compareCleared: (state) => {
      state.productIds = [];
    },
  },
});

export const { addedToCompare, removedFromCompare, compareCleared } = compareSlice.actions;
export default compareSlice.reducer;
