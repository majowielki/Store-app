import { api, unlessFailed } from './api';
import { cartApi, emptyCart } from './cart';
import type { CreateOrderFromCartRequest, HasOrdersResponse, Order, OrderStatsResponse, OrdersResponse, PricingRules } from './types';

export type PageQuery = {
  page?: number;
  pageSize?: number;
};

export const ordersApi = api.injectEndpoints({
  endpoints: (build) => ({
    /**
     * Places an order from the server cart. The Idempotency-Key makes a retry return the order
     * created the first time. The server empties the cart afterwards (through an event, so a
     * moment later); the cached cart is emptied here at once.
     */
    placeOrder: build.mutation<Order, { order: CreateOrderFromCartRequest; idempotencyKey: string }>({
      query: ({ order, idempotencyKey }) => ({
        url: '/orders/from-cart',
        method: 'POST',
        body: order,
        headers: { 'Idempotency-Key': idempotencyKey },
      }),
      invalidatesTags: unlessFailed(['Orders', 'Stats']),
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          await queryFulfilled;
          dispatch(cartApi.util.upsertQueryData('getCart', undefined, emptyCart));
        } catch {
          // reported by the error middleware
        }
      },
    }),
    getOrder: build.query<Order, number>({
      query: (id) => `/orders/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Orders', id }],
    }),
    getMyOrders: build.query<OrdersResponse, PageQuery>({
      query: (params) => ({ url: '/orders/my-orders', params }),
      providesTags: ['Orders'],
    }),
    /** The rules the order service prices by; the cart page previews the amounts with them. */
    getPricingRules: build.query<PricingRules, void>({
      query: () => '/orders/pricing-rules',
      // Configuration of the store, not data of a user: kept for the whole visit
      keepUnusedDataFor: 24 * 60 * 60,
    }),
    /** Whether the customer has ordered before: the first order is discounted. */
    getHasOrders: build.query<HasOrdersResponse, void>({
      query: () => '/orders/has-orders',
      providesTags: ['Orders'],
      extraOptions: { silent: true },
    }),

    // Admin views, served by the order service; the demo administrator gets masked customer data
    getAdminOrders: build.query<OrdersResponse, PageQuery>({
      query: (params) => ({ url: '/admin/orders', params }),
      providesTags: ['Orders'],
    }),
    getAdminOrder: build.query<Order, number>({
      query: (id) => `/admin/orders/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Orders', id }],
    }),
    getOrdersByUser: build.query<OrdersResponse, { userId: string } & PageQuery>({
      query: ({ userId, ...params }) => ({ url: `/admin/orders/by-user/${encodeURIComponent(userId)}`, params }),
      providesTags: ['Orders'],
    }),
    getOrderStats: build.query<OrderStatsResponse, { days?: number }>({
      query: (params) => ({ url: '/admin/orders/stats', params }),
      providesTags: ['Stats'],
    }),
  }),
});

export const {
  usePlaceOrderMutation,
  useGetOrderQuery,
  useGetMyOrdersQuery,
  useGetPricingRulesQuery,
  useGetHasOrdersQuery,
  useGetAdminOrdersQuery,
  useGetAdminOrderQuery,
  useGetOrdersByUserQuery,
  useGetOrderStatsQuery,
} = ordersApi;
