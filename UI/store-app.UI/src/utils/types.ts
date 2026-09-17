// Shapes of the /api/v1 contract. Success responses are the DTOs themselves (camelCase);
// errors are RFC 9457 problem responses - see ProblemDetails and utils/errorHandling.ts.

/** An error response: application/problem+json with the status code that describes it. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  /** Field-level messages of a 422 validation problem. */
  errors?: Record<string, string[]>;
}

/** One page of any listing. */
export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// PRODUCTS

export interface Product {
  id: number;
  title: string;
  description: string;
  price: number;
  salePrice?: number | null;
  discountPercent?: number | null;
  /** Price the customer pays: sale price or discounted price, otherwise the list price. */
  effectivePrice: number;
  category: string;
  company: string;
  newArrival: boolean;
  image: string;
  colors: string[];
  groups: string[];
  widthCm?: number | null;
  heightCm?: number | null;
  depthCm?: number | null;
  weightKg?: number | null;
  materials: string[];
  /** False for products deleted from the catalogue; only the admin listing returns those. */
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Body of POST /products and (partially) PUT /products/{id}; null clears an optional value on update. */
export interface ProductPayload {
  title: string;
  description: string;
  price: number;
  salePrice?: number | null;
  category: string;
  company: string;
  newArrival: boolean;
  image: string;
  colors: string[];
  groups?: string[];
  materials?: string[];
  widthCm?: number | null;
  heightCm?: number | null;
  depthCm?: number | null;
  weightKg?: number | null;
  isActive?: boolean;
}

export interface OptionItem {
  key: string;
  name: string;
}

export interface GroupWithCategories {
  key: string;
  name: string;
  categories: OptionItem[];
}

/** The values the catalogue can be filtered by; "all" comes first in every list. */
export interface ProductsMeta {
  categories: string[];
  groups: string[];
  companies: string[];
  colors: string[];
  groupCategoryMap: GroupWithCategories[];
}

export const emptyProductsMeta: ProductsMeta = {
  categories: [],
  groups: [],
  companies: [],
  colors: [],
  groupCategoryMap: [],
};

export type ProductsResponse = PagedResponse<Product>;

export interface Params {
  search?: string;
  category?: string;
  company?: string;
  color?: string;
  order?: string;
  price?: string;
  page?: number;
  group?: string;
  sale?: string;
}

/** What the products page loader hands to its components: the page, the filter values and the query. */
export type ProductsResponseWithParams = ProductsResponse & { meta: ProductsMeta; params: Params };

// CART

export interface CartItem {
  cartID: string;
  productID: number;
  image: string;
  title: string;
  // Kept as text in the local (guest) cart; the server cart carries numbers
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
  tax: number;
  orderTotal: number;
}

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
  /** True when reading the cart refreshed a line to a different catalogue price. */
  priceChanged?: boolean;
}

export interface AddCartItemRequest {
  productId: number;
  quantity: number;
  color: string;
}

export interface UpdateCartItemRequest {
  quantity: number;
}

// USERS AND AUTH

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

/** A signed-in session: the bearer token, when it expires and whose it is. */
export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: UserResponse;
}

export interface UserState {
  user: UserResponse | null;
  token: string | null;
  isLoading: boolean;
  error: string | null;
  meAttempted?: boolean;
}

export interface AdminUserResponse {
  id: string;
  email: string;
  userName: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string | null;
}

// ORDERS

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
  /** Sum of the lines before discount and delivery. */
  subtotal: number;
  discountAmount: number;
  discountReason?: string | null;
  deliveryFee: number;
  /** What the customer paid. */
  total: number;
  status: string;
  createdAt: string;
  notes?: string;
}

export type OrdersResponse = PagedResponse<Order>;

export interface CreateOrderFromCartRequest {
  userEmail: string;
  deliveryAddress?: string;
  customerName: string;
  notes?: string;
  saveAddress?: boolean;
}

export interface OrderStatsResponse {
  totalRevenue: number;
  totalOrders: number;
  daily: Array<{ bucketStart: string; orders: number; revenue: number }>;
  weekly: Array<{ bucketStart: string; orders: number; revenue: number }>;
  topProducts: Array<{ productId: number; productTitle: string; quantity: number; revenue: number }>;
}

/** Has orders for current user */
export interface HasOrdersResponse {
  hasOrders: boolean;
  ordersCount: number;
}
