import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

/** The product that was just put in the bag: the drawer suggests what goes with it. */
export interface AddedProduct {
  productId: number;
  slug: string;
  category: string;
}

export interface CartDrawerState {
  open: boolean;
  lastAdded: AddedProduct | null;
}

const initialState: CartDrawerState = { open: false, lastAdded: null };

/** The bag that slides in after "Add to bag"; the /cart page stays for the whole view. */
const cartDrawerSlice = createSlice({
  name: 'cartDrawer',
  initialState,
  reducers: {
    drawerOpened: (state, action: PayloadAction<AddedProduct | undefined>) => {
      state.open = true;
      if (action.payload) state.lastAdded = action.payload;
    },
    drawerClosed: (state) => {
      state.open = false;
    },
  },
});

export const { drawerOpened, drawerClosed } = cartDrawerSlice.actions;
export default cartDrawerSlice.reducer;

// Where the focus goes when the drawer closes: the button that opened it, which may sit in a
// dialog that has closed in between (the quick view). A DOM node, so not in the store.
let returnFocusTarget: HTMLElement | null = null;

export const setDrawerReturnFocus = (element: HTMLElement | null) => {
  returnFocusTarget = element;
};

export const takeDrawerReturnFocus = (): HTMLElement | null => {
  const element = returnFocusTarget;
  returnFocusTarget = null;
  return element?.isConnected ? element : null;
};
