// PRODUCT TYPES - MATCHING CURRENT API STRUCTURE
// Backend DTOs serialize to camelCase in JSON:
// ProductsResponse { data: ProductData[]; meta: ProductsMeta }
// SingleProductResponse { data: ProductData; meta: {} }

export interface ProductAttributes {
  category: string;
  company: string;
  createdAt: string;
  description: string;
  featured: boolean;
  image: string;
  price: string; // backend returns string for price
  publishedAt: string;
  title: string;
  updatedAt: string;
  colors: string[];
}

export interface ProductData {
  id: number;
  attributes: ProductAttributes;
}

// Backward compatible alias for legacy imports
export type Product = ProductData;

export interface PaginationMeta {
  page: number;
  pageCount: number;
  pageSize: number;
  total: number;
}

export interface ProductsMeta {
  categories: string[];
  companies: string[];
  pagination: PaginationMeta;
}

export interface ProductsResponse {
  data: ProductData[];
  meta: ProductsMeta;
}

export interface Params {
  search?: string;
  category?: string;
  company?: string;
  order?: string;
  price?: string;
  shipping?: string;
  page?: number;
}

// This must remain a type because it uses intersection (&)
export type ProductsResponseWithParams = ProductsResponse & { params: Params };

export interface SingleProductResponse {
  data: ProductData;
  meta?: Record<string, unknown>;
}

// Updated Cart Types to match your API
export interface CartItem {
  cartID: string;
  productID: number;
  image: string;
  title: string;
  price: string;
  amount: number;
  productColor: string;
  company: string;
  // Optional server item id when synced with backend
  serverItemId?: number;
}

export interface CartState {
  cartItems: CartItem[];
  numItemsInCart: number;
  cartTotal: number;
  shipping: number;
  tax: number;
  orderTotal: number;
}

// API Cart Types (matching your backend DTOs)
export interface ApiCartItemResponse {
  id: number;
  productId: number;
  title: string;
  image: string;
  price: number;
  quantity: number;
  color: string;
  company: string;
  lineTotal: number;
  createdAt: string;
  updatedAt: string;
}

export interface ApiCartResponse {
  id: number;
  userId: string;
  items: ApiCartItemResponse[];
  totalItems: number;
  total: number;
  updatedAt: string;
  isEmpty: boolean;
}

export interface AddCartItemRequest {
  productId: number;
  quantity: number;
  color: string;
}

export interface UpdateCartItemRequest {
  quantity: number;
}

// User/Auth Types (matching your backend DTOs)
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName?: string;
  lastName?: string;
}

export interface UserResponse {
  id: string;
  email: string;
  userName: string;
  firstName?: string;
  lastName?: string;
  displayName: string;
  simpleAddress?: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  accessToken?: string;
  expiresAt?: string;
  user?: UserResponse;
}

export interface UserState {
  user: UserResponse | null;
  token: string | null;
  isLoading: boolean;
  error: string | null;
  meAttempted?: boolean;
}

// OLD ORDER TYPES - DO NOT MATCH CURRENT API
// TODO: Replace with API-matching types below
// export interface Checkout {
//   name: string;
//   address: string;
//   chargeTotal: number;
//   orderTotal: string;
//   cartItems: CartItem[];
//   numItemsInCart: number;
// }

// export interface Order {
//   id: number;
//   address: string;
//   cartItems: CartItem[];
//   createdAt: string;
//   name: string;
//   numItemsInCart: number;
//   orderTotal: string;
//   publishedAt: string;
//   updatedAt: string;
// }

// // export interface OrdersMeta {
// //   pagination: Pagination;
// // }

// export interface OrdersResponse {
//   data: Order[];
//   meta: Pagination;
// }

// NEW ORDER TYPES - MATCHING CURRENT API STRUCTURE
export interface OrderItemResponse {
  id: number;
  productId: number;
  productTitle: string;
  productImage: string;
  price: number;
  quantity: number;
  color: string;
  company: string;
  lineTotal: number;
}

export interface Order {
  id: number;
  userId: string;
  userEmail: string;
  deliveryAddress?: string;
  customerName: string;
  orderItems: OrderItemResponse[];
  totalItems: number;
  orderTotal: number;
  createdAt: string;
  notes?: string;
}

// Use camelCase to match System.Text.Json default policy in our services
export interface OrdersResponse {
  orders: Order[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreateOrderFromCartRequest {
  userId: string;
  deliveryAddress?: string;
  customerName: string;
  notes?: string;
}

// CHECKOUT TYPES - NEEDS IMPLEMENTATION IN API
export interface Checkout {
  name: string;
  address: string;
  chargeTotal: number;
  orderTotal: string;
  cartItems: CartItem[];
  numItemsInCart: number;
}