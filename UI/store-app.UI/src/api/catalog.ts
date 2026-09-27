import { api, unlessFailed } from './api';
import type {
  AdminProductQuery,
  ProductDetail,
  ProductPayload,
  ProductQuery,
  ProductUpdatePayload,
  ProductsMeta,
  ProductsResponse,
} from './types';

/** The values the catalogue can be filtered by, when the filter resource is unavailable. */
export const emptyProductsMeta: ProductsMeta = {
  categories: [],
  groups: [],
  companies: [],
  colors: [],
  groupCategoryMap: [],
};

export const catalogApi = api.injectEndpoints({
  endpoints: (build) => ({
    getProducts: build.query<ProductsResponse, ProductQuery>({
      query: (params) => ({ url: '/products', params }),
      providesTags: ['Products'],
    }),
    getProduct: build.query<ProductDetail, number>({
      query: (id) => `/products/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Products', id }],
    }),
    /** One product for the admin form: with its gallery and points, and an inactive one as well. */
    getProductForAdmin: build.query<ProductDetail, number>({
      query: (id) => `/products/admin/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Products', id }],
    }),
    /** The values the catalogue can be filtered by; a page still renders without them. */
    getProductsMeta: build.query<ProductsMeta, void>({
      query: () => '/products/meta',
      providesTags: ['Products'],
      extraOptions: { silent: true },
    }),
    /** Admin listing: inactive products too, sortable by id, price, title or company. */
    getProductsAdmin: build.query<ProductsResponse, AdminProductQuery>({
      query: (params) => ({ url: '/products/admin', params }),
      providesTags: ['Products'],
    }),
    createProduct: build.mutation<ProductDetail, ProductPayload>({
      query: (body) => ({ url: '/products', method: 'POST', body }),
      invalidatesTags: unlessFailed(['Products']),
    }),
    updateProduct: build.mutation<ProductDetail, { id: number; body: ProductUpdatePayload }>({
      query: ({ id, body }) => ({ url: `/products/${id}`, method: 'PUT', body }),
      invalidatesTags: unlessFailed(['Products']),
    }),
    deleteProduct: build.mutation<void, number>({
      query: (id) => ({ url: `/products/${id}`, method: 'DELETE' }),
      invalidatesTags: unlessFailed(['Products']),
    }),
  }),
});

export const {
  useGetProductsQuery,
  useGetProductQuery,
  useGetProductForAdminQuery,
  useGetProductsMetaQuery,
  useGetProductsAdminQuery,
  useCreateProductMutation,
  useUpdateProductMutation,
  useDeleteProductMutation,
} = catalogApi;
