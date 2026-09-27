import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

/**
 * The discount code typed in the cart, carried to the checkout. The order service checks it
 * (useDiscountCode) and applies it once more when the order is placed.
 */
export interface DiscountCodeState {
  code: string | null;
}

const initialState: DiscountCodeState = { code: null };

const discountCodeSlice = createSlice({
  name: 'discountCode',
  initialState,
  reducers: {
    codeApplied: (state, action: PayloadAction<string>) => {
      const code = action.payload.trim().toUpperCase();
      state.code = code.length > 0 ? code : null;
    },
    codeRemoved: (state) => {
      state.code = null;
    },
  },
});

export const { codeApplied, codeRemoved } = discountCodeSlice.actions;
export default discountCodeSlice.reducer;

export const DISCOUNT_CODE_STORAGE_KEY = 'discountCode';

/** The code typed earlier in this tab, so a reload of the checkout keeps it. */
export const loadDiscountCode = (): DiscountCodeState => {
  try {
    const code = sessionStorage.getItem(DISCOUNT_CODE_STORAGE_KEY);
    return { code: code && code.length <= 32 ? code : null };
  } catch {
    return initialState;
  }
};

export const saveDiscountCode = (state: DiscountCodeState) => {
  try {
    if (state.code) sessionStorage.setItem(DISCOUNT_CODE_STORAGE_KEY, state.code);
    else sessionStorage.removeItem(DISCOUNT_CODE_STORAGE_KEY);
  } catch {
    // storage unavailable (private mode): the code lives until the page is left
  }
};
