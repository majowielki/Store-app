import { customFetch } from './customFetch';
import type { 
  ApiCartResponse, 
  AddCartItemRequest, 
  UpdateCartItemRequest,
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  UserResponse,
  ProductsResponse,
  ProductData,
  SingleProductResponse,
  OrdersResponse,
  Order,
  CreateOrderFromCartRequest
} from './types';

// Auth API functions
export const authApi = {
  login: async (credentials: LoginRequest): Promise<AuthResponse> => {
    const { data } = await customFetch.post<AuthResponse>('/auth/login', credentials);
    return data;
  },

  register: async (userData: RegisterRequest): Promise<AuthResponse> => {
    const { data } = await customFetch.post<AuthResponse>('/auth/register', userData);
    return data;
  },

  // IMPLEMENTED IN BACKEND BUT NOT USED IN FRONTEND YET
  demoLogin: async (): Promise<AuthResponse> => {
    const { data } = await customFetch.post<AuthResponse>('/auth/demo-login');
    return data;
  },

  // NOT FULLY IMPLEMENTED IN BACKEND - JUST RETURNS 200 OK
  logout: async (): Promise<void> => {
    await customFetch.post('/auth/logout');
  },

  // NOT IMPLEMENTED IN BACKEND YET
  getCurrentUser: async (): Promise<UserResponse> => {
    const { data } = await customFetch.get<UserResponse>('/auth/me');
    return data;
  },

  // NOT IMPLEMENTED IN BACKEND YET
  refreshToken: async (): Promise<AuthResponse> => {
    const { data } = await customFetch.post<AuthResponse>('/auth/refresh');
    return data;
  }
};

// Cart API functions
export const cartApi = {
  getCart: async (): Promise<ApiCartResponse> => {
    const { data } = await customFetch.get<ApiCartResponse>('/cart');
    return data;
  },

  addItem: async (item: AddCartItemRequest): Promise<ApiCartResponse> => {
    const { data } = await customFetch.post<ApiCartResponse>('/cart/items', item);
    return data;
  },

  updateItem: async (itemId: number, update: UpdateCartItemRequest): Promise<ApiCartResponse> => {
    const { data } = await customFetch.put<ApiCartResponse>(`/cart/items/${itemId}`, update);
    return data;
  },

  removeItem: async (itemId: number): Promise<ApiCartResponse> => {
    const { data } = await customFetch.delete<ApiCartResponse>(`/cart/items/${itemId}`);
    return data;
  },

  clearCart: async (): Promise<void> => {
    await customFetch.delete('/cart');
  },

  syncWithServer: async (payload: { items: { productId: number; quantity: number; color: string }[] }): Promise<ApiCartResponse> => {
    const { data } = await customFetch.post<ApiCartResponse>('/cart/sync', payload);
    return data;
  }
};

// PRODUCT API - MATCHING CURRENT BACKEND IMPLEMENTATION
export const productApi = {
  getProducts: async (params?: {
    search?: string;
    category?: string;
    company?: string;
    page?: number;
    pageSize?: number;
  }): Promise<ProductsResponse> => {
    const { data } = await customFetch.get<ProductsResponse>('/products', { params });
    return data;
  },

  getProduct: async (id: number): Promise<ProductData> => {
    const { data } = await customFetch.get<SingleProductResponse>(`/products/${id}`);
    return data.data;
  },
  // Note: featured/category/company specific endpoints were removed;
  // use getProducts with appropriate query params instead.
};

// ORDER API - MATCHING CURRENT BACKEND IMPLEMENTATION
export const orderApi = {
  createOrderFromCart: async (orderData: CreateOrderFromCartRequest): Promise<Order> => {
    const { data } = await customFetch.post<Order>('/orders/from-cart', orderData);
    return data;
  },

  getOrder: async (id: number): Promise<Order> => {
    const { data } = await customFetch.get<Order>(`/orders/${id}`);
    return data;
  },

  getMyOrders: async (page = 1, pageSize = 20): Promise<OrdersResponse> => {
    const { data } = await customFetch.get<OrdersResponse>('/orders/my-orders', {
      params: { page, pageSize }
    });
    return data;
  }
};

// MISSING API ENDPOINTS - NEED IMPLEMENTATION IN BACKEND:
// 
// Product API missing endpoints:
// - POST /products (admin only) - exists in backend but not in frontend
// - PUT /products/{id} (admin only) - exists in backend but not in frontend  
// - DELETE /products/{id} (admin only) - exists in backend but not in frontend
//
// Auth API missing endpoints:
// - POST /auth/demo-login - exists in backend but not in frontend
// - POST /auth/refresh - not implemented in backend
// - GET /auth/me - not implemented in backend
// - POST /auth/logout - not fully implemented in backend
//
// Cart API missing endpoints:
// - POST /cart/sync - not implemented in backend (custom frontend method)
//
// Order API missing endpoints:  
// - GET /orders (admin only) - not implemented in backend
// - PUT /orders/{id} (admin only) - not implemented in backend
// - DELETE /orders/{id} (admin only) - not implemented in backend
//
// Additional APIs that might be needed:
// - Profile management (update user profile, change password, etc.)
// - Admin dashboard APIs
// - Analytics/reporting APIs
// - File upload APIs (for product images)
// - Search suggestions API
// - Product reviews/ratings API
// - Wishlist API
// - Discount/coupon API
