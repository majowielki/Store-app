import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

/**
 * A line of the cart a visitor fills before signing in. It lives in this browser only; at
 * sign-in it is merged into the server cart once and forgotten (see sessionThunks.ts).
 */
export interface GuestCartItem {
  productId: number;
  title: string;
  image: string;
  company: string;
  color: string;
  /** What the product page showed when the line was added - the sale price on a sale. */
  unitPrice: number;
  quantity: number;
}

export interface GuestCartState {
  items: GuestCartItem[];
}

export const GUEST_CART_STORAGE_KEY = 'guestCart';

const isItem = (value: unknown): value is GuestCartItem =>
  typeof value === 'object' &&
  value !== null &&
  typeof (value as GuestCartItem).productId === 'number' &&
  typeof (value as GuestCartItem).color === 'string' &&
  typeof (value as GuestCartItem).quantity === 'number' &&
  typeof (value as GuestCartItem).unitPrice === 'number';

/** The cart left in this browser by an earlier visit; anything unreadable is treated as empty. */
export const loadGuestCart = (): GuestCartState => {
  try {
    const raw = localStorage.getItem(GUEST_CART_STORAGE_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : null;
    const items = parsed && typeof parsed === 'object' && Array.isArray((parsed as GuestCartState).items)
      ? (parsed as GuestCartState).items.filter(isItem)
      : [];
    return { items };
  } catch {
    return { items: [] };
  }
};

export const saveGuestCart = (state: GuestCartState): void => {
  try {
    localStorage.setItem(GUEST_CART_STORAGE_KEY, JSON.stringify(state));
  } catch {
    // Storage may be full or disabled; the cart then lasts for the page only
  }
};

const sameLine = (a: Pick<GuestCartItem, 'productId' | 'color'>, b: Pick<GuestCartItem, 'productId' | 'color'>) =>
  a.productId === b.productId && a.color === b.color;

const guestCartSlice = createSlice({
  name: 'guestCart',
  initialState: (): GuestCartState => ({ items: [] }),
  reducers: {
    /** Adds a line, or raises the quantity of the line with the same product and colour. */
    itemAdded: (state, action: PayloadAction<GuestCartItem>) => {
      const existing = state.items.find((item) => sameLine(item, action.payload));
      if (existing) {
        existing.quantity += action.payload.quantity;
        existing.unitPrice = action.payload.unitPrice;
      } else {
        state.items.push(action.payload);
      }
    },
    quantityChanged: (state, action: PayloadAction<{ productId: number; color: string; quantity: number }>) => {
      const item = state.items.find((line) => sameLine(line, action.payload));
      if (item) item.quantity = Math.max(1, action.payload.quantity);
    },
    itemRemoved: (state, action: PayloadAction<{ productId: number; color: string }>) => {
      state.items = state.items.filter((item) => !sameLine(item, action.payload));
    },
    cleared: (state) => {
      state.items = [];
    },
  },
});

export const { itemAdded, quantityChanged, itemRemoved, cleared } = guestCartSlice.actions;

export default guestCartSlice.reducer;
