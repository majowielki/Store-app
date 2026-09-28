import { api } from './api';
import type { CardPayload, OrderPayment, Payment } from './types';

/**
 * Paying an order: the order service opens its payment (the same one on every call), then the
 * browser pays it with a card straight at the payment service and answers the 3-D Secure window.
 * Silent: the payment page shows every refusal next to the form, not in a toast.
 */
export const paymentsApi = api.injectEndpoints({
  endpoints: (build) => ({
    startPayment: build.mutation<OrderPayment, number>({
      query: (orderId) => ({ url: `/orders/${orderId}/payment`, method: 'POST' }),
      extraOptions: { silent: true },
    }),
    confirmPayment: build.mutation<Payment, { paymentId: string; card: CardPayload }>({
      query: ({ paymentId, card }) => ({ url: `/payments/${paymentId}/confirm`, method: 'POST', body: card }),
      extraOptions: { silent: true },
    }),
    authenticatePayment: build.mutation<Payment, { paymentId: string; approve: boolean }>({
      query: ({ paymentId, approve }) => ({ url: `/payments/${paymentId}/authenticate`, method: 'POST', body: { approve } }),
      extraOptions: { silent: true },
    }),
  }),
});

export const { useStartPaymentMutation, useConfirmPaymentMutation, useAuthenticatePaymentMutation } = paymentsApi;
