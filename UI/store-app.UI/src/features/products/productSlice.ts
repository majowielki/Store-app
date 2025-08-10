import { createSlice, createAsyncThunk, type PayloadAction } from "@reduxjs/toolkit";
import type { Product, ProductsResponse } from "@/utils/types";
import { productApi } from "@/utils/api";
import { getErrorMessage } from "@/utils/errorHandling";
import { toast } from "@/hooks/use-toast";

interface ProductState {
  products: Product[];
  currentProduct: Product | null;
  isLoading: boolean;
  error: string | null;
  // Pagination
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  // Filters
  filters: {
    search?: string;
    category?: string;
    company?: string;
  };
}

const initialState: ProductState = {
  products: [],
  currentProduct: null,
  isLoading: false,
  error: null,
  totalCount: 0,
  page: 1,
  pageSize: 20,
  totalPages: 0,
  hasNextPage: false,
  hasPreviousPage: false,
  filters: {}
};

// Async thunks
export const fetchProductsAsync = createAsyncThunk(
  'products/fetchProducts',
  async (params: {
    search?: string;
    category?: string;
    company?: string;
    page?: number;
    pageSize?: number;
  } = {}, { rejectWithValue }) => {
    try {
  const response = await productApi.getProducts(params);
      return { response, params };
    } catch (error: unknown) {
      const message = getErrorMessage(error);
      return rejectWithValue(message);
    }
  }
);

export const fetchProductAsync = createAsyncThunk(
  'products/fetchProduct',
  async (id: number, { rejectWithValue }) => {
    try {
  const product = await productApi.getProduct(id);
      return product;
    } catch (error: unknown) {
      const message = getErrorMessage(error);
      return rejectWithValue(message);
    }
  }
);

const productSlice = createSlice({
  name: 'products',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    clearCurrentProduct: (state) => {
      state.currentProduct = null;
    },
    setFilters: (state, action: PayloadAction<{
      search?: string;
      category?: string;
      company?: string;
    }>) => {
      state.filters = { ...state.filters, ...action.payload };
    },
    clearFilters: (state) => {
      state.filters = {};
    }
  },
  extraReducers: (builder) => {
    // Fetch Products
    builder
      .addCase(fetchProductsAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchProductsAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.error = null;
  const { response, params } = action.payload as { response: ProductsResponse; params: Record<string, unknown> };
  state.products = response.data as unknown as Product[];
  const p = response.meta.pagination;
  state.totalCount = p.total;
  state.page = p.page;
  state.pageSize = p.pageSize;
  state.totalPages = p.pageCount;
  state.hasNextPage = p.page < p.pageCount;
  state.hasPreviousPage = p.page > 1;
        state.filters = { ...state.filters, ...params };
      })
      .addCase(fetchProductsAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
        toast({
          description: `Failed to fetch products: ${action.payload}`,
          variant: 'destructive'
        });
      })

    // Fetch Single Product
    builder
      .addCase(fetchProductAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchProductAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.error = null;
        state.currentProduct = action.payload;
      })
      .addCase(fetchProductAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
        toast({
          description: `Failed to fetch product: ${action.payload}`,
          variant: 'destructive'
        });
      })
  ;
  },
});

export const { 
  clearError, 
  clearCurrentProduct, 
  setFilters, 
  clearFilters 
} = productSlice.actions;

// Export async actions
export { 
  fetchProductsAsync as fetchProducts,
  fetchProductAsync as fetchProduct,
};

export default productSlice.reducer;
