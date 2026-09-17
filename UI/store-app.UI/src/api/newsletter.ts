import { api } from './api';

/** Served by the gateway itself; the subscription is only logged there for now. */
export const newsletterApi = api.injectEndpoints({
  endpoints: (build) => ({
    subscribeToNewsletter: build.mutation<void, string>({
      query: (email) => ({ url: '/newsletter/subscribe', method: 'POST', body: { email } }),
    }),
  }),
});

export const { useSubscribeToNewsletterMutation } = newsletterApi;
