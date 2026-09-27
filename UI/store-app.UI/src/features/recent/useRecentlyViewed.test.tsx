import { renderHook, waitFor } from '@testing-library/react';
import { http } from 'msw';
import type { ReactNode } from 'react';
import { Provider } from 'react-redux';
import { describe, expect, it } from 'vitest';
import { createAppStore } from '@/store';
import { product } from '@/test/fixtures';
import { api, json, page } from '@/test/handlers';
import { server } from '@/test/server';
import recentlyViewedReducer, { productViewed } from './recentlyViewedSlice';
import { useRecentlyViewed, useTrackProductView } from './useRecentlyViewed';

const lamp = product({ id: 8, title: 'Lamp', slug: 'lamp' });
const rug = product({ id: 9, title: 'Rug', slug: 'rug' });

describe('recently viewed', () => {
  it('keeps the last eight products, the latest first, each once', () => {
    let state = recentlyViewedReducer(undefined, { type: 'init' });
    for (const id of [1, 2, 3, 4, 5, 6, 7, 8, 9, 3]) state = recentlyViewedReducer(state, productViewed(id));

    expect(state.productIds).toEqual([3, 9, 8, 7, 6, 5, 4, 2]);
  });

  it('records the product on the page and leaves it out of its own strip', async () => {
    const asked: string[] = [];
    server.use(
      http.get(api('/products'), ({ request }) => {
        asked.push(new URL(request.url).searchParams.get('ids') ?? '');
        return json(page([rug, lamp]));
      }),
    );
    const store = createAppStore({ recentlyViewed: { productIds: [8, 9] } });
    const wrapper = ({ children }: { children: ReactNode }) => <Provider store={store}>{children}</Provider>;

    const { result } = renderHook(
      () => {
        useTrackProductView(7);
        return useRecentlyViewed(7);
      },
      { wrapper },
    );

    await waitFor(() => expect(result.current.products.map((p) => p.title)).toEqual(['Lamp', 'Rug']));
    expect(store.getState().recentlyViewed.productIds).toEqual([7, 8, 9]);
    expect(asked.at(-1)).toBe('8,9');
  });
});
