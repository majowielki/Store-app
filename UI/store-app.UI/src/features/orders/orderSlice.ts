import { createSlice, createAsyncThunk } from "@reduxjs/toolkit";
import type { Order, CreateOrderFromCartRequest } from "@/utils/types";
import { orderApi } from "@/utils/api";
import { getErrorMessage } from "@/utils/errorHandling";
import { toast } from "@/hooks/use-toast";

interface OrdersResponse {
  orders: Order[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

interface OrderState {
  orders: Order[];
  currentOrder: Order | null;
  isLoading: boolean;
  error: string | null;
  // Pagination
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

const initialState: OrderState = {
  orders: [],
  currentOrder: null,
  isLoading: false,
  error: null,
  totalCount: 0,
  page: 1,
  pageSize: 20,
  totalPages: 0,
  hasNextPage: false,
  hasPreviousPage: false
};

// Async thunks
export const createOrderFromCartAsync = createAsyncThunk(
  'orders/createOrderFromCart',
  async (orderData: CreateOrderFromCartRequest, { rejectWithValue }) => {
    try {
      const order = await orderApi.createOrderFromCart(orderData);
      return order;
    } catch (error: unknown) {
      const message = getErrorMessage(error);
      return rejectWithValue(message);
    }
  }
);

export const fetchOrderAsync = createAsyncThunk(
  'orders/fetchOrder',
  async (id: number, { rejectWithValue }) => {
    try {
      const order = await orderApi.getOrder(id);
      return order;
    } catch (error: unknown) {
      const message = getErrorMessage(error);
      return rejectWithValue(message);
    }
  }
);

export const fetchMyOrdersAsync = createAsyncThunk(
  'orders/fetchMyOrders',
  async (params: { page?: number; pageSize?: number } = {}, { rejectWithValue }) => {
    try {
      const response: OrdersResponse = await orderApi.getMyOrders(params.page, params.pageSize);
      return response;
    } catch (error: unknown) {
      const message = getErrorMessage(error);
      return rejectWithValue(message);
    }
  }
);

const orderSlice = createSlice({
  name: 'orders',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    clearCurrentOrder: (state) => {
      state.currentOrder = null;
    },
    clearOrders: (state) => {
      state.orders = [];
      state.totalCount = 0;
      state.page = 1;
      state.totalPages = 0;
      state.hasNextPage = false;
      state.hasPreviousPage = false;
    }
  },
  extraReducers: (builder) => {
    // Create Order from Cart
    builder
      .addCase(createOrderFromCartAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(createOrderFromCartAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.error = null;
        state.currentOrder = action.payload;
        // Add to orders list if not already there
        const exists = state.orders.find(order => order.id === action.payload.id);
        if (!exists) {
          state.orders.unshift(action.payload); // Add to beginning
        }
        toast({ description: 'Order created successfully!' });
      })
      .addCase(createOrderFromCartAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
        toast({
          description: `Failed to create order: ${action.payload}`,
          variant: 'destructive'
        });
      })

    // Fetch Single Order
    builder
      .addCase(fetchOrderAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchOrderAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.error = null;
        state.currentOrder = action.payload;
      })
      .addCase(fetchOrderAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
        toast({
          description: `Failed to fetch order: ${action.payload}`,
          variant: 'destructive'
        });
      })

    // Fetch My Orders
    builder
      .addCase(fetchMyOrdersAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchMyOrdersAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.error = null;
        state.orders = action.payload.orders;
        state.totalCount = action.payload.totalCount;
        state.page = action.payload.page;
        state.pageSize = action.payload.pageSize;
        state.totalPages = action.payload.totalPages;
        state.hasNextPage = action.payload.hasNextPage;
        state.hasPreviousPage = action.payload.hasPreviousPage;
      })
      .addCase(fetchMyOrdersAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
        toast({
          description: `Failed to fetch orders: ${action.payload}`,
          variant: 'destructive'
        });
      });
  },
});

export const { 
  clearError, 
  clearCurrentOrder, 
  clearOrders 
} = orderSlice.actions;

// Export async actions
export { 
  createOrderFromCartAsync as createOrderFromCart,
  fetchOrderAsync as fetchOrder,
  fetchMyOrdersAsync as fetchMyOrders
};

export default orderSlice.reducer;
