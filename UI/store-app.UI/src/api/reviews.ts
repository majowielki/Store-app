import { api, unlessFailed } from './api';
import type {
  AdminReviewQuery,
  AdminReviewsResponse,
  ModerationPayload,
  ModerationResult,
  MyProductReview,
  Review,
  ReviewPayload,
  ReviewQuery,
  ReviewSummary,
  ReviewsResponse,
  ShopReviewStats,
} from './types';

/**
 * Product reviews, served by the review service (ADR 012). Anyone reads the published ones and
 * the ratings; a signed-in customer writes a review of a product they paid for, sees their own in
 * any state and reports others'; the administrators moderate. Writing and reporting are silent:
 * their forms show what the API refused next to the fields.
 */
export const reviewsApi = api.injectEndpoints({
  endpoints: (build) => ({
    getReviews: build.query<ReviewsResponse, ReviewQuery>({
      query: (params) => ({ url: '/reviews', params }),
      providesTags: ['Reviews'],
    }),
    getReviewSummary: build.query<ReviewSummary, number>({
      query: (productId) => `/reviews/products/${productId}/summary`,
      providesTags: ['Reviews'],
    }),
    /** The shop's own rating, every published review whatever the product. Silent: the About page shows a dash without it. */
    getShopReviewStats: build.query<ShopReviewStats, void>({
      query: () => '/reviews/stats',
      providesTags: ['Reviews'],
      extraOptions: { silent: true },
    }),
    /** Whether the signed-in customer may review the product, and their review of it in any state. */
    getMyProductReview: build.query<MyProductReview, number>({
      query: (productId) => `/reviews/products/${productId}/mine`,
      providesTags: ['Reviews'],
    }),
    /** The signed-in customer's reviews of every product (on a demo account: this sign-in session's). */
    getMyReviews: build.query<Review[], void>({
      query: () => '/reviews/mine',
      providesTags: ['Reviews'],
    }),
    createReview: build.mutation<Review, ReviewPayload>({
      query: (body) => ({ url: '/reviews', method: 'POST', body }),
      invalidatesTags: unlessFailed(['Reviews']),
      extraOptions: { silent: true },
    }),
    reportReview: build.mutation<void, { id: string; reason?: string }>({
      query: ({ id, reason }) => ({ url: `/reviews/${id}/report`, method: 'POST', body: { reason: reason || null } }),
      invalidatesTags: unlessFailed(['Reviews']),
      extraOptions: { silent: true },
    }),
    /** The moderation queue; the demo administrator gets it without the texts nobody has approved. */
    getAdminReviews: build.query<AdminReviewsResponse, AdminReviewQuery>({
      query: (params) => ({ url: '/reviews/admin', params }),
      providesTags: ['Reviews'],
    }),
    moderateReviews: build.mutation<ModerationResult, ModerationPayload>({
      query: (body) => ({ url: '/reviews/admin/moderate', method: 'POST', body }),
      invalidatesTags: unlessFailed(['Reviews']),
    }),
  }),
});

export const {
  useGetReviewsQuery,
  useGetReviewSummaryQuery,
  useGetShopReviewStatsQuery,
  useGetMyProductReviewQuery,
  useGetMyReviewsQuery,
  useCreateReviewMutation,
  useReportReviewMutation,
  useGetAdminReviewsQuery,
  useModerateReviewsMutation,
} = reviewsApi;
