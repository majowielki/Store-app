import { combineReducers, configureStore } from '@reduxjs/toolkit';
import { setupListeners } from '@reduxjs/toolkit/query';
import { api } from './api/api';
import { errorToasts } from './api/errorToasts';
import { onSessionEnded } from './api/session';
import cartDrawerReducer from './features/cart/cartDrawerSlice';
import discountCodeReducer, { loadDiscountCode, saveDiscountCode } from './features/cart/discountCodeSlice';
import guestCartReducer, { loadGuestCart, saveGuestCart } from './features/cart/guestCartSlice';
import compareReducer, { loadCompare, saveCompare } from './features/compare/compareSlice';
import recentlyViewedReducer, { loadRecentlyViewed, saveRecentlyViewed } from './features/recent/recentlyViewedSlice';
import guestWishlistReducer, { loadGuestWishlist, saveGuestWishlist } from './features/wishlist/guestWishlistSlice';
import sessionReducer, { sessionEnded } from './features/session/sessionSlice';
import themeReducer from './features/theme/themeSlice';
import { toast } from './hooks/use-toast';

// Everything the API answers lives in the RTK Query cache; the store keeps only what the
// UI owns itself: the session (who is signed in), the visitor's cart and wishlist, the discount
// code typed in the cart, whether the cart drawer is open, the products viewed lately and the
// ones picked for comparison, and the theme.
const rootReducer = combineReducers({
  [api.reducerPath]: api.reducer,
  session: sessionReducer,
  guestCart: guestCartReducer,
  guestWishlist: guestWishlistReducer,
  discountCode: discountCodeReducer,
  cartDrawer: cartDrawerReducer,
  recentlyViewed: recentlyViewedReducer,
  compare: compareReducer,
  theme: themeReducer,
});

export type RootState = ReturnType<typeof rootReducer>;

export const createAppStore = (preloadedState?: Partial<RootState>) =>
  configureStore({
    reducer: rootReducer,
    preloadedState,
    middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(api.middleware, errorToasts),
  });

export type AppStore = ReturnType<typeof createAppStore>;
export type AppDispatch = AppStore['dispatch'];

export const store = createAppStore({
  guestCart: loadGuestCart(),
  guestWishlist: loadGuestWishlist(),
  discountCode: loadDiscountCode(),
  recentlyViewed: loadRecentlyViewed(),
  compare: loadCompare(),
});

// Refetch on focus and reconnect, for the queries that ask for it
setupListeners(store.dispatch);

/** Saves a part of the state whenever it changes (the reducers return a new object then). */
const persist = <T>(select: (state: RootState) => T, save: (value: T) => void) => {
  let last = select(store.getState());
  store.subscribe(() => {
    const next = select(store.getState());
    if (next !== last) {
      last = next;
      save(next);
    }
  });
};

// The visitor's cart and wishlist, the products viewed and compared outlive the page; the typed
// discount code a reload of the tab
persist((state) => state.guestCart, saveGuestCart);
persist((state) => state.guestWishlist, saveGuestWishlist);
persist((state) => state.discountCode, saveDiscountCode);
persist((state) => state.recentlyViewed, saveRecentlyViewed);
persist((state) => state.compare, saveCompare);

// A refused refresh means the session is over for the whole app, whichever request found out
onSessionEnded(() => {
  if (store.getState().session.user) {
    toast({ description: 'Your session has expired. Please sign in again.' });
  }
  store.dispatch(sessionEnded());
});
