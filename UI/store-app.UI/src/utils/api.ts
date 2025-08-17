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
  CreateOrderFromCartRequest,
  HasOrdersResponse
} from './types';

// Backend stats response type (matches actual API response)
export type BackendStatsResponse = {
  totalRevenue: number;
  totalOrders: number;
  daily: Array<{ bucketStart: string; orders: number; revenue: number }>;
  weekly: Array<{ bucketStart: string; orders: number; revenue: number }>;
  topProducts: Array<{ productId: number; productTitle: string; quantity: number; revenue: number }>;
};

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
    const { data } = await customFetch.post<AuthResponse>('/auth/demo-login', {});
    return data;
  },

  demoAdminLogin: async (): Promise<AuthResponse> => {
    const { data } = await customFetch.post<AuthResponse>('/auth/demo-admin-login', {});
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

  // Update current user's simple address
  updateMyAddress: async (simpleAddress: string): Promise<UserResponse> => {
    const { data } = await customFetch.put<UserResponse>('/auth/me/address', { simpleAddress });
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
    group?: 'all' | 'furniture' | 'bathroom' | 'kids' | 'garden';
    sale?: boolean;
    price?: string; // "100,500" or "100-500"
  }): Promise<ProductsResponse> => {
    const { data } = await customFetch.get<ProductsResponse>('/products', { params });
    return data;
  },

  getProduct: async (id: number): Promise<ProductData> => {
    const { data } = await customFetch.get<SingleProductResponse>(`/products/${id}`);
    return data.data;
  },
  getProductsMeta: async (): Promise<ProductsResponse['meta']> => {
    const { data } = await customFetch.get<{ meta: ProductsResponse['meta'] }>(`/products/meta`);
    return data.meta;
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
  },

  // Admin: list all orders with pagination (existing)
  getAllOrders: async (page = 1, pageSize = 20): Promise<OrdersResponse> => {
    const { data } = await customFetch.get<OrdersResponse>('/admin/orders', { params: { page, pageSize } });
    return data;
  },

  // Admin override to fetch any order by id
  getOrderAdmin: async (id: number): Promise<Order> => {
    const { data } = await customFetch.get<Order>(`/admin/orders/${id}`);
    return data;
  },

  // Admin: list orders by user id
  getOrdersByUser: async (userId: string, page = 1, pageSize = 20): Promise<OrdersResponse> => {
    const { data } = await customFetch.get<OrdersResponse>(`/orders/by-user/${userId}`, { params: { page, pageSize } });
    return data;
  },

  // Admin stats endpoint
  getAdminStats: async (days = 30): Promise<BackendStatsResponse> => {
    const { data } = await customFetch.get<BackendStatsResponse>('/orders/stats', { params: { days } });
    return data;
  },

  // User promo eligibility: has orders
  getHasOrders: async (): Promise<HasOrdersResponse> => {
    const { data } = await customFetch.get<HasOrdersResponse>('/orders/has-orders');
    return data;
  },
};

// Newsletter API - FRONTEND HOOK CONTRACT
// Backend to implement: POST /newsletter/subscribe { email: string }
export const newsletterApi = {
  subscribe: async (email: string): Promise<void> => {
    await customFetch.post('/newsletter/subscribe', { email });
  },
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

// Identity Admin API
export const identityAdminApi = {
  // GET /api/admin/users?search=&isActive=&page=&pageSize=
  // Backend users response type (matches actual API response)
  getUsers: async (params?: { search?: string; isActive?: boolean; page?: number; pageSize?: number }): Promise<{ items: UserResponse[]; totalCount: number; page: number; pageSize: number; totalPages?: number }> => {
    const { data } = await customFetch.get<{ items: UserResponse[]; totalCount: number; page: number; pageSize: number; totalPages?: number }>('http://localhost:5001/api/admin/users', { params });
    return data;
  },

  // GET /api/admin/users/{id}
  getUser: async (id: string): Promise<UserResponse> => {
    const { data } = await customFetch.get<UserResponse>(`/admin/users/${id}`);
    return data;
  },

  // GET /api/admin/users/{id}/orders?page=&pageSize=
  getUserOrders: async (id: string, page = 1, pageSize = 20): Promise<OrdersResponse> => {
    const { data } = await customFetch.get<OrdersResponse>(`/admin/users/${id}/orders`, { params: { page, pageSize } });
    return data;
  },
};
