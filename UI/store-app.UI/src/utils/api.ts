import { customFetch } from './customFetch';
import type {
  ApiCartResponse,
  AddCartItemRequest,
  UpdateCartItemRequest,
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  UserResponse,
  AdminUserResponse,
  ProductsResponse,
  ProductsMeta,
  Product,
  ProductPayload,
  ProductUpdatePayload,
  ProductQuery,
  AdminProductQuery,
  OrdersResponse,
  Order,
  OrderStatsResponse,
  CreateOrderFromCartRequest,
  HasOrdersResponse,
  PagedResponse,
} from './types';

// Every function returns the response body as the API sends it (see utils/types.ts);
// failures reject with an axios error whose response.data is a problem (utils/errorHandling.ts).

export const authApi = {
  login: async (credentials: LoginRequest): Promise<AuthResponse> => {
    const { data } = await customFetch.post<AuthResponse>('/auth/login', credentials);
    return data;
  },

  register: async (userData: RegisterRequest): Promise<AuthResponse> => {
    const { data } = await customFetch.post<AuthResponse>('/auth/register', userData);
    return data;
  },

  demoLogin: async (): Promise<AuthResponse> => {
    const { data } = await customFetch.post<AuthResponse>('/auth/demo-login');
    return data;
  },

  demoAdminLogin: async (): Promise<AuthResponse> => {
    const { data } = await customFetch.post<AuthResponse>('/auth/demo-admin-login');
    return data;
  },

  logout: async (): Promise<void> => {
    await customFetch.post('/auth/logout');
  },

  /** The signed-in user's profile; null without a stored token or for an anonymous answer (204). */
  getCurrentUser: async (): Promise<UserResponse | null> => {
    const token = localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
    if (!token) return null;
    const { data, status } = await customFetch.get<UserResponse>('/auth/me');
    return status === 204 ? null : data;
  },

  updateMyAddress: async (simpleAddress: string): Promise<UserResponse> => {
    const { data } = await customFetch.put<UserResponse>('/auth/me/address', { simpleAddress });
    return data;
  },

  refreshToken: async (token: string): Promise<AuthResponse> => {
    const { data } = await customFetch.post<AuthResponse>('/auth/refresh', { token });
    return data;
  },
};

// Every change to the cart answers with the whole cart as it is afterwards
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
  },
};

export const productApi = {
  getProducts: async (params?: ProductQuery): Promise<ProductsResponse> => {
    const { data } = await customFetch.get<ProductsResponse>('/products', { params });
    return data;
  },

  getProduct: async (id: number): Promise<Product> => {
    const { data } = await customFetch.get<Product>(`/products/${id}`);
    return data;
  },

  /** The values the catalogue can be filtered by. */
  getProductsMeta: async (): Promise<ProductsMeta> => {
    const { data } = await customFetch.get<ProductsMeta>('/products/meta');
    return data;
  },

  /** Admin listing: inactive products too, sortable by id, price, title or company. */
  getProductsAdmin: async (params: AdminProductQuery): Promise<ProductsResponse> => {
    const { data } = await customFetch.get<ProductsResponse>('/products/admin', { params });
    return data;
  },

  createProduct: async (payload: ProductPayload): Promise<Product> => {
    const { data } = await customFetch.post<Product>('/products', payload);
    return data;
  },

  updateProduct: async (id: number, payload: ProductUpdatePayload): Promise<Product> => {
    const { data } = await customFetch.put<Product>(`/products/${id}`, payload);
    return data;
  },

  deleteProduct: async (id: number): Promise<void> => {
    await customFetch.delete(`/products/${id}`);
  },
};

export const orderApi = {
  /** Places an order; the Idempotency-Key makes a retry return the order created the first time. */
  createOrderFromCart: async (orderData: CreateOrderFromCartRequest, idempotencyKey?: string): Promise<Order> => {
    const { data } = await customFetch.post<Order>('/orders/from-cart', orderData, {
      headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : undefined,
    });
    return data;
  },

  getOrder: async (id: number): Promise<Order> => {
    const { data } = await customFetch.get<Order>(`/orders/${id}`);
    return data;
  },

  getMyOrders: async (page = 1, pageSize = 20): Promise<OrdersResponse> => {
    const { data } = await customFetch.get<OrdersResponse>('/orders/my-orders', { params: { page, pageSize } });
    return data;
  },

  /** User promo eligibility: has orders */
  getHasOrders: async (): Promise<HasOrdersResponse> => {
    const { data } = await customFetch.get<HasOrdersResponse>('/orders/has-orders');
    return data;
  },

  // Admin views, served by the order service; the demo administrator gets masked customer data
  getAllOrders: async (page = 1, pageSize = 20): Promise<OrdersResponse> => {
    const { data } = await customFetch.get<OrdersResponse>('/admin/orders', { params: { page, pageSize } });
    return data;
  },

  getOrderAdmin: async (id: number): Promise<Order> => {
    const { data } = await customFetch.get<Order>(`/admin/orders/${id}`);
    return data;
  },

  getOrdersByUser: async (userId: string, page = 1, pageSize = 20): Promise<OrdersResponse> => {
    const { data } = await customFetch.get<OrdersResponse>(`/admin/orders/by-user/${encodeURIComponent(userId)}`, { params: { page, pageSize } });
    return data;
  },

  getAdminStats: async (days = 30): Promise<OrderStatsResponse> => {
    const { data } = await customFetch.get<OrderStatsResponse>('/admin/orders/stats', { params: { days } });
    return data;
  },
};

export const newsletterApi = {
  subscribe: async (email: string): Promise<void> => {
    await customFetch.post('/newsletter/subscribe', { email });
  },
};

export const identityAdminApi = {
  getUsers: async (params?: { search?: string; isActive?: boolean; page?: number; pageSize?: number }): Promise<PagedResponse<AdminUserResponse>> => {
    const { data } = await customFetch.get<PagedResponse<AdminUserResponse>>('/admin/users', { params });
    return data;
  },

  getUser: async (id: string): Promise<UserResponse> => {
    const { data } = await customFetch.get<UserResponse>(`/admin/users/${id}`);
    return data;
  },
};
