// The API contract as the UI names it. Every type here is an alias of a schema generated from
// the services' OpenAPI documents (src/api/schema/*.ts, "npm run api:generate"); nothing about
// the wire format is written by hand, so a contract change fails "tsc" instead of a page.
import type { components as Audit } from './schema/audit';
import type { components as Cart } from './schema/cart';
import type { components as Catalog, paths as CatalogPaths } from './schema/catalog';
import type { components as Identity } from './schema/identity';
import type { components as Orders } from './schema/orders';

/** An error response: application/problem+json with the status code that describes it. */
export type ProblemDetails = Catalog['schemas']['StoreProblemDetails'];

// Listings share one shape; the generator names it per item type
export type ProductsResponse = Catalog['schemas']['ProductResponsePagedResponse'];
export type OrdersResponse = Orders['schemas']['OrderResponsePagedResponse'];
export type AdminUsersResponse = Identity['schemas']['AdminUserResponsePagedResponse'];
export type AuditLogsResponse = Audit['schemas']['AuditLogPagedResponse'];
export type PagedResponse<T> = Omit<ProductsResponse, 'items'> & { items: T[] };

// Catalogue
export type Product = Catalog['schemas']['ProductResponse'];
export type ProductCategory = Catalog['schemas']['Category'];
export type ProductCompany = Catalog['schemas']['Company'];
export type ProductPayload = Catalog['schemas']['CreateProductRequest'];
export type ProductUpdatePayload = Catalog['schemas']['UpdateProductRequest'];
export type ProductsMeta = Catalog['schemas']['ProductsMeta'];
export type GroupWithCategories = Catalog['schemas']['GroupWithCategories'];
export type OptionItem = Catalog['schemas']['OptionItem'];
export type ProductQuery = NonNullable<CatalogPaths['/api/v1/products']['get']['parameters']['query']>;
export type AdminProductQuery = NonNullable<CatalogPaths['/api/v1/products/admin']['get']['parameters']['query']>;

// Cart
export type ApiCartResponse = Cart['schemas']['CartResponse'];
export type ApiCartItemResponse = Cart['schemas']['CartItemResponse'];
export type AddCartItemRequest = Cart['schemas']['AddCartItemRequest'];
export type UpdateCartItemRequest = Cart['schemas']['UpdateCartItemRequest'];
export type SyncCartRequest = Cart['schemas']['SyncCartRequest'];

// Users and sessions
export type LoginRequest = Identity['schemas']['LoginRequest'];
export type RegisterRequest = Identity['schemas']['RegisterRequest'];
export type UpdateAddressRequest = Identity['schemas']['UpdateAddressRequest'];
export type UserResponse = Identity['schemas']['UserResponse'];
export type AuthResponse = Identity['schemas']['AuthResponse'];
export type AdminUserResponse = Identity['schemas']['AdminUserResponse'];

// Orders
export type Order = Orders['schemas']['OrderResponse'];
export type OrderItemResponse = Orders['schemas']['OrderItemResponse'];
export type CreateOrderFromCartRequest = Orders['schemas']['CreateOrderFromCartRequest'];
export type OrderStatsResponse = Orders['schemas']['OrderStatsResponse'];
export type HasOrdersResponse = Orders['schemas']['HasOrdersResponse'];

// Audit trail
export type AuditLog = Audit['schemas']['AuditLog'];
