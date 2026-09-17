import { combineReducers, configureStore } from '@reduxjs/toolkit';
import { setupListeners } from '@reduxjs/toolkit/query';
import { api } from './api/api';
import { errorToasts } from './api/errorToasts';
import { onSessionEnded } from './api/session';
import guestCartReducer, { loadGuestCart, saveGuestCart } from './features/cart/guestCartSlice';
import sessionReducer, { sessionEnded } from './features/session/sessionSlice';
import themeReducer from './features/theme/themeSlice';
import { toast } from './hooks/use-toast';

// Everything the API answers lives in the RTK Query cache; the store keeps only what the
// UI owns itself: the session (who is signed in), the visitor's cart and the theme.
const rootReducer = combineReducers({
  [api.reducerPath]: api.reducer,
  session: sessionReducer,
  guestCart: guestCartReducer,
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

export const store = createAppStore({ guestCart: loadGuestCart() });

// Refetch on focus and reconnect, for the queries that ask for it
setupListeners(store.dispatch);

// The visitor's cart outlives the page
let lastGuestCart = store.getState().guestCart;
store.subscribe(() => {
  const guestCart = store.getState().guestCart;
  if (guestCart !== lastGuestCart) {
    lastGuestCart = guestCart;
    saveGuestCart(guestCart);
  }
});

// A refused refresh means the session is over for the whole app, whichever request found out
onSessionEnded(() => {
  if (store.getState().session.user) {
    toast({ description: 'Your session has expired. Please sign in again.' });
  }
  store.dispatch(sessionEnded());
});
