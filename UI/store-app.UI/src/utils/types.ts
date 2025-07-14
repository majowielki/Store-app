export interface ProductsResponse {
  data: Product[];
  meta: ProductsMeta;
}

export interface Product {
  id: number;
  category: string;
  company: string;
  description: string;
  featured: boolean;
  image: string;
  price: string;
  shipping: boolean;
  title: string;
  colors: string[];
}

export interface ProductsMeta {
  categories: string[];
  companies: string[];
  pagination: Pagination;
}

export interface Pagination {
  page: number;
  pageCount: number;
  pageSize: number;
  total: number;
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
  data: Product;
}

export interface CartItem {
  cartID: string;
  productID: number;
  image: string;
  title: string;
  price: string;
  amount: number;
  productColor: string;
  company: string;
}

export interface CartState {
  cartItems: CartItem[];
  numItemsInCart: number;
  cartTotal: number;
  shipping: number;
  tax: number;
  orderTotal: number;
}

export interface Checkout {
  name: string;
  address: string;
  chargeTotal: number;
  orderTotal: string;
  cartItems: CartItem[];
  numItemsInCart: number;
}

export interface Order {
  id: number;
  address: string;
  cartItems: CartItem[];
  createdAt: string;
  name: string;
  numItemsInCart: number;
  orderTotal: string;
  publishedAt: string;
  updatedAt: string;
}

// export interface OrdersMeta {
//   pagination: Pagination;
// }

export interface OrdersResponse {
  data: Order[];
  meta: Pagination;
}