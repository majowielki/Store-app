import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

/**
 * The wishlist of a visitor: product ids kept in this browser, the one added last first. At
 * sign-in it is merged into the account's list once and forgotten (see sessionThunks.ts).
 */
export interface GuestWishlistState {
  productIds: number[];
}

export const GUEST_WISHLIST_STORAGE_KEY = 'guestWishlist';
const MAX_ITEMS = 100;

export const loadGuestWishlist = (): GuestWishlistState => {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(GUEST_WISHLIST_STORAGE_KEY) ?? 'null');
    const ids = parsed && typeof parsed === 'object' && Array.isArray((parsed as GuestWishlistState).productIds) ? (parsed as GuestWishlistState).productIds : [];
    return { productIds: ids.filter((id): id is number => Number.isInteger(id) && id > 0).slice(0, MAX_ITEMS) };
  } catch {
    return { productIds: [] };
  }
};

export const saveGuestWishlist = (state: GuestWishlistState): void => {
  try {
    localStorage.setItem(GUEST_WISHLIST_STORAGE_KEY, JSON.stringify(state));
  } catch {
    // Storage may be full or disabled; the list then lasts for the page only
  }
};

const guestWishlistSlice = createSlice({
  name: 'guestWishlist',
  initialState: (): GuestWishlistState => ({ productIds: [] }),
  reducers: {
    wished: (state, action: PayloadAction<number>) => {
      if (!state.productIds.includes(action.payload)) state.productIds = [action.payload, ...state.productIds].slice(0, MAX_ITEMS);
    },
    unwished: (state, action: PayloadAction<number>) => {
      state.productIds = state.productIds.filter((id) => id !== action.payload);
    },
    wishlistCleared: (state) => {
      state.productIds = [];
    },
  },
});

export const { wished, unwished, wishlistCleared } = guestWishlistSlice.actions;
export default guestWishlistSlice.reducer;
