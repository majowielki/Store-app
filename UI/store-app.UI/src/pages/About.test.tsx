import { screen, waitFor, within } from '@testing-library/react';
import { http } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api, json, problemResponse } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import About from './About';

/** Waits until the figure printed over a label of the About page's numbers reads <text>. */
const expectFigure = (label: string, text: string) =>
  waitFor(() => {
    const term = screen.getByText(label, { selector: 'dt' });
    expect(within(term.parentElement!).getByRole('definition')).toHaveTextContent(text);
  });

describe('About', () => {
  beforeEach(() => {
    // The delivery window of the fixtures (Sep 30 – Oct 2) seen from Monday, Sep 28
    vi.useFakeTimers({ now: new Date(2026, 8, 28, 10, 0), toFake: ['Date'] });
    // Numbers do not count up for a visitor who asked for less motion: the test reads them at once
    vi.mocked(window.matchMedia).mockImplementation(
      (query: string) =>
        ({ matches: query.includes('prefers-reduced-motion'), media: query, addEventListener: vi.fn(), removeEventListener: vi.fn() }) as unknown as MediaQueryList,
    );
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('prints the numbers the services give, not made-up ones', async () => {
    server.use(
      http.get(api('/orders/stats'), () => json({ paidOrders: 42 })),
      http.get(api('/reviews/stats'), () => json({ averageRating: 4.64, reviewCount: 312 })),
    );

    renderWithStore(<About />);

    await expectFigure('orders paid for', '42');
    await expectFigure('average of 312 reviews', '4.6/5');
    await expectFigure('days from order to door', '2–4');
  });

  it('shows a dash for a number its service could not give', async () => {
    server.use(
      http.get(api('/orders/stats'), () => problemResponse(503, 'The order service is down')),
      http.get(api('/reviews/stats'), () => json({ averageRating: 0, reviewCount: 0 })),
    );

    renderWithStore(<About />);

    await expectFigure('days from order to door', '2–4');
    await expectFigure('orders paid for', '—');
    await expectFigure('average rating', '—');
  });
});
