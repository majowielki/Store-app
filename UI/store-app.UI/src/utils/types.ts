// State the UI keeps for itself. The API contract lives in src/api/types.ts (aliases of the
// generated OpenAPI schemas) and is re-exported here so the rest of the app imports one module.
import type { ProductsMeta, ProductsResponse, UserResponse } from '@/api/types';

export * from '@/api/types';

export const emptyProductsMeta: ProductsMeta = {
  categories: [],
  groups: [],
  companies: [],
  colors: [],
  groupCategoryMap: [],
};

/** Query string of the products page, as the router hands it to the loader. */
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

// The guest cart, kept in the browser until sign-in
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

export interface UserState {
  /** The profile, cached across reloads; the session itself is the in-memory access token plus the refresh cookie */
  user: UserResponse | null;
  isLoading: boolean;
  error: string | null;
  /** True once the session was restored or refused after a page load */
  sessionChecked: boolean;
}
