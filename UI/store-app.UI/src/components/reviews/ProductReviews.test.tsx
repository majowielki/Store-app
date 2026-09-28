import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import type { MyProductReview, Review } from '@/api/types';
import ProductCard from '@/components/ProductCard';
import { order, product, review, reviewSummary, user as customer } from '@/test/fixtures';
import { api, json, page, problemResponse } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import OrderDetail from '@/pages/OrderDetail';
import ProductReviews from './ProductReviews';

const second = review({ id: 'b7a1c2d3-0000-4000-8000-000000000002', authorName: 'Tom W.', rating: 4, title: 'Nearly perfect', body: 'Lovely piece, with one small niggle about the drawer.' });

let listRequests: URLSearchParams[];

beforeEach(() => {
  listRequests = [];
  server.use(
    http.get(api('/reviews'), ({ request }) => {
      const params = new URL(request.url).searchParams;
      listRequests.push(params);
      const rating = params.get('rating');
      return json(page([review(), second].filter((r) => !rating || r.rating === Number(rating))));
    }),
    http.get(api('/reviews/products/7/summary'), () => json(reviewSummary())),
  );
});

describe('the reviews of a product', () => {
  it('shows the rating, how the stars fall and the reviews, and filters by a number of stars', async () => {
    const user = userEvent.setup();
    renderWithStore(<ProductReviews productId={7} productTitle="Oak Table" />);

    expect(await screen.findByRole('article', { name: 'Review by Marta S.' })).toHaveTextContent('The oak has a lovely grain');
    expect(screen.getByRole('article', { name: 'Review by Tom W.' })).toHaveTextContent('Verified purchase');
    expect(screen.getByText('Based on 2 reviews')).toBeInTheDocument();
    expect(screen.getAllByRole('img', { name: 'Rated 4.5 out of 5' }).length).toBeGreaterThan(0);

    await user.click(screen.getByRole('button', { name: '4 stars: 1 review' }));

    expect(await screen.findByRole('button', { name: /4 stars only/ })).toBeInTheDocument();
    expect(screen.queryByRole('article', { name: 'Review by Marta S.' })).not.toBeInTheDocument();
    expect(listRequests.at(-1)?.get('rating')).toBe('4');
    // Nothing bought, nothing to report: a visitor is asked to sign in
    expect(screen.getByRole('link', { name: 'Sign in to review' })).toHaveAttribute('href', '/login');
    expect(screen.queryByRole('button', { name: /Report/ })).not.toBeInTheDocument();
  });

  it('lets a customer who bought the product write a review, which then waits for moderation', async () => {
    const user = userEvent.setup();
    let mine: MyProductReview = { productId: 7, canReview: true };
    let sent: unknown;
    server.use(
      http.get(api('/reviews/products/7/mine'), () => json(mine)),
      http.post(api('/reviews'), async ({ request }) => {
        sent = await request.json();
        const written: Review = review({ id: 'b7a1c2d3-0000-4000-8000-000000000009', authorName: 'Anna N.', status: 'pending', rating: 4, body: 'Solid oak and a lovely warm colour in our room.' });
        mine = { productId: 7, canReview: false, reason: 'alreadyReviewed', review: written };
        return json(written, { status: 201 });
      }),
    );
    renderWithStore(<ProductReviews productId={7} productTitle="Oak Table" />, { user: customer });

    await user.click(await screen.findByRole('button', { name: 'Write a review' }));
    const form = await screen.findByRole('form', { name: 'Review of Oak Table' });
    await user.type(within(form).getByLabelText('Review'), 'Too short');
    await user.click(within(form).getByRole('button', { name: 'Send review' }));
    expect(within(form).getByRole('alert')).toHaveTextContent('Choose how many stars');

    await user.click(within(form).getByRole('radio', { name: '4 stars' }));
    await user.click(within(form).getByRole('button', { name: 'Send review' }));
    expect(within(form).getByRole('alert')).toHaveTextContent('at least 20 characters');

    await user.type(within(form).getByLabelText('Review'), ' - solid oak and a lovely warm colour.');
    await user.type(within(form).getByLabelText('Title (optional)'), 'Warm and solid');
    await user.click(within(form).getByRole('button', { name: 'Send review' }));

    expect(await screen.findByText(/Your review is awaiting moderation/)).toBeInTheDocument();
    expect(sent).toEqual({ productId: 7, rating: 4, title: 'Warm and solid', body: 'Too short - solid oak and a lovely warm colour.' });
    await user.click(screen.getByRole('button', { name: 'Done' }));
    expect(await screen.findByText(/Awaiting moderation - only you can see it/)).toBeInTheDocument();
    expect(screen.getByText('Thank you for reviewing this piece.')).toBeInTheDocument();
  });

  it('shows the automatic checks refusing a review next to the form', async () => {
    const user = userEvent.setup();
    server.use(
      http.get(api('/reviews/products/7/mine'), () => json({ productId: 7, canReview: true })),
      http.post(api('/reviews'), () =>
        problemResponse(422, 'One or more validation errors occurred.', { Body: ['Links are not allowed in a review.'] }),
      ),
    );
    renderWithStore(<ProductReviews productId={7} productTitle="Oak Table" />, { user: customer });

    await user.click(await screen.findByRole('button', { name: 'Write a review' }));
    const form = await screen.findByRole('form', { name: 'Review of Oak Table' });
    await user.click(within(form).getByRole('radio', { name: '5 stars' }));
    await user.type(within(form).getByLabelText('Review'), 'Cheaper at www.example.com, same table');
    await user.click(within(form).getByRole('button', { name: 'Send review' }));

    expect(await within(form).findByRole('alert')).toHaveTextContent('Links are not allowed in a review.');
  });

  it('tells the author why their review was not published and offers to write it again', async () => {
    const user = userEvent.setup();
    const rejected = review({ id: 'b7a1c2d3-0000-4000-8000-000000000010', status: 'rejected', rejectionReason: 'It describes another product', body: 'This review is about a chair I bought elsewhere.' });
    server.use(http.get(api('/reviews/products/7/mine'), () => json({ productId: 7, canReview: true, review: rejected })));
    renderWithStore(<ProductReviews productId={7} productTitle="Oak Table" />, { user: customer });

    expect(await screen.findByText(/Not published: It describes another product/)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Write it again' }));

    const form = await screen.findByRole('form', { name: 'Review of Oak Table' });
    expect(within(form).getByLabelText('Review')).toHaveValue('This review is about a chair I bought elsewhere.');
  });

  it('lets a signed-in customer report another review', async () => {
    const user = userEvent.setup();
    let reported: { id: string; body: unknown } | undefined;
    server.use(
      http.post(api('/reviews/:id/report'), async ({ params, request }) => {
        reported = { id: String(params.id), body: await request.json() };
        return new Response(null, { status: 204 });
      }),
    );
    renderWithStore(<ProductReviews productId={7} productTitle="Oak Table" />, { user: customer });

    await user.click(await screen.findByRole('button', { name: 'Report the review by Tom W.' }));
    const form = await screen.findByRole('form', { name: 'Report this review' });
    await user.type(within(form).getByLabelText(/What is wrong with it/), 'Advertises another shop');
    await user.click(within(form).getByRole('button', { name: 'Report review' }));

    expect(await screen.findAllByText(/The review is hidden until our team has looked at it/)).not.toHaveLength(0);
    expect(reported).toEqual({ id: second.id, body: { reason: 'Advertises another shop' } });
  });
});

describe('ratings and reviews elsewhere in the shop', () => {
  it('shows the rating on a product card only once it has reviews', () => {
    const { unmount } = renderWithStore(<ProductCard product={product({ ratingAverage: 4.25, ratingCount: 8 })} />);
    expect(screen.getByRole('img', { name: 'Rated 4.3 out of 5' })).toBeInTheDocument();
    expect(screen.getByText('(8)')).toBeInTheDocument();
    unmount();

    renderWithStore(<ProductCard product={product({ ratingAverage: 0, ratingCount: 0 })} />);
    expect(screen.queryByRole('img', { name: /Rated/ })).not.toBeInTheDocument();
  });

  it('offers to review each piece of a paid order and says where a written review stands', async () => {
    const lamp = { ...order().orderItems[0], id: 2, productId: 8, productTitle: 'Brass Lamp' };
    server.use(
      http.get(api('/orders/100'), () => json(order({ status: 'Paid', orderItems: [order().orderItems[0], lamp] }))),
      http.get(api('/reviews/mine'), () => json([review({ productId: 8, status: 'pending' })])),
    );
    renderWithStore(<OrderDetail />, { user: customer, route: '/orders/100', path: '/orders/:id' });

    expect(await screen.findByRole('button', { name: 'Write a review: Oak Table' })).toBeInTheDocument();
    expect(await screen.findByText('Awaiting moderation')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Write a review: Brass Lamp' })).not.toBeInTheDocument();
  });
});
