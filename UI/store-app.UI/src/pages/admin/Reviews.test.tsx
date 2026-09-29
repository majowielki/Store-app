import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { admin, adminReview, product } from '@/test/fixtures';
import { api, json, page } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import Reviews from './Reviews';

const reported = adminReview({
  id: 'c8b2d3e4-0000-4000-8000-000000000003',
  productId: 8,
  authorName: 'Lucas B.',
  status: 'published',
  reported: true,
  reportCount: 1,
  reportReasons: ['Advertises another shop'],
  source: 'seed',
});

let decisions: unknown[];

beforeEach(() => {
  decisions = [];
  server.use(
    http.get(api('/reviews/admin'), () => json(page([adminReview(), reported]))),
    http.get(api('/products'), () => json(page([product(), product({ id: 8, title: 'Brass Lamp', slug: 'brass-lamp' })]))),
    http.post(api('/reviews/admin/moderate'), async ({ request }) => {
      const body = (await request.json()) as { ids: string[] };
      decisions.push(body);
      return json({ moderated: body.ids.length, notFound: [] });
    }),
  );
});

describe('the moderation queue', () => {
  it('lists the reviews waiting for a decision with their products and reports', async () => {
    renderWithStore(<Reviews />, { user: admin });

    const pending = await screen.findByRole('listitem', { name: 'Review of Oak Table by Anna N.' });
    expect(pending).toHaveTextContent('Pending');
    const flagged = await screen.findByRole('listitem', { name: 'Review of Brass Lamp by Lucas B.' });
    expect(flagged).toHaveTextContent('Reported');
    expect(flagged).toHaveTextContent('Reports: Advertises another shop');
    expect(flagged).toHaveTextContent('Seeded');
  });

  it('shows what the model made of a review and why', async () => {
    server.use(
      http.get(api('/reviews/admin'), () =>
        json(
          page([
            adminReview({ modelVerdict: 'doubtful', modelReason: 'It asks readers to get in touch.' }),
            adminReview({ id: 'c8b2d3e4-0000-4000-8000-000000000004', productId: 8, authorName: 'Lucas B.', status: 'published', modelVerdict: 'clean', modelReason: 'An opinion about the lamp.', moderatedAt: '2026-09-29T10:00:00Z', moderatedBy: null }),
          ]),
        ),
      ),
    );
    renderWithStore(<Reviews />, { user: admin });

    const held = await screen.findByRole('listitem', { name: 'Review of Oak Table by Anna N.' });
    expect(held).toHaveTextContent('Model: doubtful');
    expect(held).toHaveTextContent('Model: It asks readers to get in touch.');
    const published = await screen.findByRole('listitem', { name: 'Review of Brass Lamp by Lucas B.' });
    expect(published).toHaveTextContent('Model: clean');
    expect(published).toHaveTextContent(/published by the model/);
  });

  it('approves one review', async () => {
    const user = userEvent.setup();
    renderWithStore(<Reviews />, { user: admin });

    await user.click(await screen.findByRole('button', { name: 'Approve the review by Anna N.' }));

    expect(await screen.findAllByText('1 review published')).not.toHaveLength(0);
    expect(decisions).toEqual([{ ids: [adminReview().id], decision: 'approve', reason: null }]);
  });

  it('rejects only with a reason, which the author will see', async () => {
    const user = userEvent.setup();
    renderWithStore(<Reviews />, { user: admin });

    await user.click(await screen.findByRole('button', { name: 'Reject the review by Anna N.' }));
    const dialog = await screen.findByRole('dialog', { name: 'Reject the review' });
    expect(within(dialog).getByRole('button', { name: 'Reject' })).toBeDisabled();

    await user.click(within(dialog).getByRole('button', { name: 'Not about the product' }));
    await user.click(within(dialog).getByRole('button', { name: 'Reject' }));

    expect(await screen.findAllByText('1 review rejected')).not.toHaveLength(0);
    expect(decisions).toEqual([{ ids: [adminReview().id], decision: 'reject', reason: 'Not about the product' }]);
  });

  it('decides on every selected review at once', async () => {
    const user = userEvent.setup();
    renderWithStore(<Reviews />, { user: admin });

    await user.click(await screen.findByRole('checkbox', { name: 'Select every review on this page' }));
    expect(screen.getByText('2 selected')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Approve selected' }));

    expect(await screen.findAllByText('2 reviews published')).not.toHaveLength(0);
    expect(decisions).toEqual([{ ids: [adminReview().id, reported.id], decision: 'approve', reason: null }]);
  });
});
