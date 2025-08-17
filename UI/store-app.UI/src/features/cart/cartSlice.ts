import { createSlice, createAsyncThunk, type PayloadAction } from "@reduxjs/toolkit";
import { type CartItem, type CartState, type ApiCartResponse, type AddCartItemRequest } from "@/utils";
import { cartApi } from "@/utils/api";
import { getErrorMessage } from "@/utils/errorHandling";
import { toast } from "@/hooks/use-toast";

const defaultState: CartState = {
  cartItems: [],
  numItemsInCart: 0,
  cartTotal: 0,
  tax: 0,
  orderTotal: 0,
};

const getCartFromLocalStorage = (): CartState => {
  const cart = localStorage.getItem("cart");
  if (cart) {
    return JSON.parse(cart);
  } else {
    return defaultState;
  }
};

// Helper function to convert API cart response to local cart format
const convertApiCartToLocal = (apiCart: ApiCartResponse): CartState => {
  const cartItems: CartItem[] = apiCart.items.map(item => ({
    cartID: `${item.productId}-${item.color}`,
    productID: item.productId,
    image: item.image,
    title: item.title,
    price: item.price.toString(),
    amount: item.quantity,
    productColor: item.color,
    company: item.company,
    serverItemId: item.id,
  }));

  const cartTotal = Number(apiCart.total);
  const tax = cartTotal >= 299 ? 0 : 10; // using tax field to store delivery cost
  const orderTotal = cartTotal + tax;

  return {
    cartItems,
    numItemsInCart: apiCart.totalItems,
    cartTotal,
    tax,
    orderTotal,
  };
};

// Async thunks for API calls
const fetchCartAsync = createAsyncThunk(
  'cart/fetchCart',
  async (_, { rejectWithValue }) => {
    try {
      const apiCart = await cartApi.getCart();
      return convertApiCartToLocal(apiCart);
    } catch (error: unknown) {
      const message = getErrorMessage(error);
      return rejectWithValue(message);
    }
  }
);

const addItemToServerAsync = createAsyncThunk(
  'cart/addItemToServer',
  async (item: AddCartItemRequest, { rejectWithValue }) => {
    try {
      const apiCart = await cartApi.addItem(item);
      return convertApiCartToLocal(apiCart);
    } catch (error: unknown) {
      const message = getErrorMessage(error);
      return rejectWithValue(message);
    }
  }
);

const updateItemOnServerAsync = createAsyncThunk(
  'cart/updateItemOnServer',
  async ({ itemId, quantity }: { itemId: number; quantity: number }, { rejectWithValue }) => {
    try {
      const apiCart = await cartApi.updateItem(itemId, { quantity });
      return convertApiCartToLocal(apiCart);
    } catch (error: unknown) {
      const message = getErrorMessage(error);
      return rejectWithValue(message);
    }
  }
);

const removeItemFromServerAsync = createAsyncThunk(
  'cart/removeItemFromServer',
  async (itemId: number, { rejectWithValue }) => {
    try {
      const apiCart = await cartApi.removeItem(itemId);
      return convertApiCartToLocal(apiCart);
    } catch (error: unknown) {
      const message = getErrorMessage(error);
      return rejectWithValue(message);
    }
  }
);

const clearCartOnServerAsync = createAsyncThunk(
  'cart/clearCartOnServer',
  async (_, { rejectWithValue }) => {
    try {
      await cartApi.clearCart();
      return defaultState;
    } catch (error: unknown) {
      const message = getErrorMessage(error);
      return rejectWithValue(message);
    }
  }
);

// Merge local cart (guest) into server cart after login, then return server cart
const mergeLocalCartToServerAsync = createAsyncThunk(
  'cart/mergeLocalToServer',
  async (_: void, { getState, rejectWithValue }) => {
    try {
      const state = getState() as { cartState: CartState };
      const localItems = state.cartState.cartItems;

      // 1) Get current server cart
      const serverCart = await cartApi.getCart();

      // If nothing local to merge, return server as-is
      if (!localItems || localItems.length === 0) {
        return convertApiCartToLocal(serverCart);
      }

      // 2) Merge by adding local quantities onto server via sync endpoint
      //    Server-side will increment existing items and create missing ones.
      const syncPayload = {
        items: localItems.map((item) => ({
          productId: item.productID,
          quantity: item.amount,
          color: item.productColor,
        })),
      };

      const mergedServerCart = await cartApi.syncWithServer(syncPayload);
      return convertApiCartToLocal(mergedServerCart);
    } catch (error: unknown) {
      const message = getErrorMessage(error);
      return rejectWithValue(message);
    }
  }
);

const cartSlice = createSlice({
  name: "cart",
  initialState: getCartFromLocalStorage(),
  reducers: {
    // Local-only actions (for offline usage)
    addItem: (state, action: PayloadAction<CartItem>) => {
      const newCartItem = action.payload;
      const item = state.cartItems.find((i) => i.cartID === newCartItem.cartID);
      if (item) {
        item.amount += newCartItem.amount;
      } else {
        state.cartItems.push(newCartItem);
      }
      state.numItemsInCart += newCartItem.amount;
      state.cartTotal += Number(newCartItem.price) * newCartItem.amount;
      cartSlice.caseReducers.calculateTotals(state);
      toast({ description: "Item added to cart" });
    },
    clearCart: () => {
      localStorage.setItem("cart", JSON.stringify(defaultState));
      return defaultState;
    },
    removeItem: (state, action: PayloadAction<string>) => {
      const cartID = action.payload;
      const cartItem = state.cartItems.find((i) => i.cartID === cartID);
      if (!cartItem) return;
      state.cartItems = state.cartItems.filter((i) => i.cartID !== cartID);
      state.numItemsInCart -= cartItem.amount;
      state.cartTotal -= Number(cartItem.price) * cartItem.amount;
      cartSlice.caseReducers.calculateTotals(state);
      toast({ description: "Item removed from the cart" });
    },
    editItem: (
      state,
      action: PayloadAction<{ cartID: string; amount: number }>
    ) => {
      const { cartID, amount } = action.payload;
      const cartItem = state.cartItems.find((i) => i.cartID === cartID);
      if (!cartItem) return;

      state.numItemsInCart += amount - cartItem.amount;
      state.cartTotal += Number(cartItem.price) * (amount - cartItem.amount);
      cartItem.amount = amount;

      cartSlice.caseReducers.calculateTotals(state);
      toast({ description: "Amount Updated" });
    },
    calculateTotals: (state) => {
      // Delivery: 10 when subtotal < 299, otherwise free (0)
      state.tax = state.cartTotal >= 299 ? 0 : 10; // tax field represents delivery
      state.orderTotal = state.cartTotal + state.tax;
      localStorage.setItem("cart", JSON.stringify(state));
    },
    // Action to sync local state with server response
    syncWithServer: (state, action: PayloadAction<CartState>) => {
      const serverState = action.payload;
      state.cartItems = serverState.cartItems;
      state.numItemsInCart = serverState.numItemsInCart;
      state.cartTotal = serverState.cartTotal;
  // Recalculate delivery locally to ensure policy is applied
  state.tax = state.cartTotal >= 299 ? 0 : 10;
  state.orderTotal = state.cartTotal + state.tax;
      localStorage.setItem("cart", JSON.stringify(state));
    },
  },
  extraReducers: (builder) => {
    // Fetch Cart
    builder
      .addCase(fetchCartAsync.fulfilled, (state, action) => {
        const serverState = action.payload;
        state.cartItems = serverState.cartItems;
        state.numItemsInCart = serverState.numItemsInCart;
        state.cartTotal = serverState.cartTotal;
  state.tax = state.cartTotal >= 299 ? 0 : 10;
  state.orderTotal = state.cartTotal + state.tax;
        localStorage.setItem("cart", JSON.stringify(state));
      })
      .addCase(fetchCartAsync.rejected, (_, action) => {
        toast({
          description: `Failed to fetch cart: ${action.payload}`,
          variant: 'destructive'
        });
      })

    // Add Item to Server
    builder
      .addCase(addItemToServerAsync.fulfilled, (state, action) => {
        const serverState = action.payload;
        state.cartItems = serverState.cartItems;
        state.numItemsInCart = serverState.numItemsInCart;
        state.cartTotal = serverState.cartTotal;
  state.tax = state.cartTotal >= 299 ? 0 : 10;
  state.orderTotal = state.cartTotal + state.tax;
        localStorage.setItem("cart", JSON.stringify(state));
        toast({ description: "Item added to cart" });
      })
      .addCase(addItemToServerAsync.rejected, (_, action) => {
        toast({
          description: `Failed to add item: ${action.payload}`,
          variant: 'destructive'
        });
      })

    // Update Item on Server
    builder
      .addCase(updateItemOnServerAsync.fulfilled, (state, action) => {
        const serverState = action.payload;
        state.cartItems = serverState.cartItems;
        state.numItemsInCart = serverState.numItemsInCart;
        state.cartTotal = serverState.cartTotal;
  state.tax = state.cartTotal >= 299 ? 0 : 10;
  state.orderTotal = state.cartTotal + state.tax;
        localStorage.setItem("cart", JSON.stringify(state));
        toast({ description: "Item updated" });
      })
      .addCase(updateItemOnServerAsync.rejected, (_, action) => {
        toast({
          description: `Failed to update item: ${action.payload}`,
          variant: 'destructive'
        });
      })

    // Remove Item from Server
    builder
      .addCase(removeItemFromServerAsync.fulfilled, (state, action) => {
        const serverState = action.payload;
        state.cartItems = serverState.cartItems;
        state.numItemsInCart = serverState.numItemsInCart;
        state.cartTotal = serverState.cartTotal;
        state.tax = serverState.tax;
        state.orderTotal = serverState.orderTotal;
        localStorage.setItem("cart", JSON.stringify(state));
        toast({ description: "Item removed from cart" });
      })
      .addCase(removeItemFromServerAsync.rejected, (_, action) => {
        toast({
          description: `Failed to remove item: ${action.payload}`,
          variant: 'destructive'
        });
      })

    // Clear Cart on Server
    builder
      .addCase(clearCartOnServerAsync.fulfilled, (state) => {
        state.cartItems = [];
        state.numItemsInCart = 0;
        state.cartTotal = 0;
  state.tax = 0;
  state.orderTotal = 0;
        localStorage.setItem("cart", JSON.stringify(state));
        toast({ description: "Cart cleared" });
      })
      .addCase(clearCartOnServerAsync.rejected, (_, action) => {
        toast({
          description: `Failed to clear cart: ${action.payload}`,
          variant: 'destructive'
        });
      })

    // Merge local cart to server
    builder
      .addCase(mergeLocalCartToServerAsync.fulfilled, (state, action) => {
        const serverState = action.payload;
        state.cartItems = serverState.cartItems;
        state.numItemsInCart = serverState.numItemsInCart;
        state.cartTotal = serverState.cartTotal;
  state.tax = state.cartTotal >= 299 ? 0 : 10;
  state.orderTotal = state.cartTotal + state.tax;
  localStorage.setItem("cart", JSON.stringify(state));
        toast({ description: "Cart synced" });
      })
      .addCase(mergeLocalCartToServerAsync.rejected, (_, action) => {
        toast({
          description: `Failed to sync cart: ${action.payload}`,
          variant: 'destructive'
        });
      });
  },
});

export const { 
  addItem, 
  clearCart, 
  removeItem, 
  editItem, 
  calculateTotals, 
  syncWithServer 
} = cartSlice.actions;

// Export async actions
export { 
  fetchCartAsync as fetchCart,
  addItemToServerAsync as addItemToServer,
  updateItemOnServerAsync as updateItemOnServer,
  removeItemFromServerAsync as removeItemFromServer,
  clearCartOnServerAsync as clearCartOnServer
};

export { mergeLocalCartToServerAsync as mergeLocalCartToServer };

export default cartSlice.reducer;
