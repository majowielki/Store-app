import { configureStore } from "@reduxjs/toolkit";
import themeReducer from "./features/theme/themeSlice";
import cartReducer from "./features/cart/cartSlice";
import userReducer from "./features/user/userSlice";
import productReducer from "./features/products/productSlice";
import orderReducer from "./features/orders/orderSlice";
import { clearUser } from "./features/user/userSlice";
import { onSessionEnded } from "./utils/session";

export const store = configureStore({
  reducer: {
    themeState: themeReducer,
    cartState: cartReducer,
    userState: userReducer,
    productState: productReducer,
    orderState: orderReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;

export type ReduxStore = {
  getState: () => RootState;
  dispatch: AppDispatch;
};

// A refused refresh means the session is over for the whole app, whichever request found out
onSessionEnded(() => store.dispatch(clearUser()));
