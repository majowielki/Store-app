import { act, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { useCartActions } from '@/features/cart/useCart';
import { api } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import { useCountProductView } from './useShopEvents';

let sent: unknown[];

beforeEach(() => {
  sent = [];
  server.use(
    http.post(api('/shop-events'), async ({ request }) => {
      sent.push(await request.json());
      return new HttpResponse(null, { status: 202 });
    }),
  );
});

const ProductPage = ({ id }: { id: number }) => {
  useCountProductView(id);
  return null;
};

describe('the purchase funnel counts', () => {
  it('count a product page once for each product it shows', async () => {
    const { rerender } = renderWithStore(<ProductPage id={4} />);
    await waitFor(() => expect(sent).toHaveLength(1));

    rerender(<ProductPage id={4} />);
    rerender(<ProductPage id={9} />);

    await waitFor(() => expect(sent).toEqual([
      { kind: 'productViewed', productId: 4 },
      { kind: 'productViewed', productId: 9 },
    ]));
  });

  it('count a product put in the bag, by a visitor too', async () => {
    let add: ReturnType<typeof useCartActions>['add'] | undefined;
    const Bag = () => {
      add = useCartActions().add;
      return null;
    };
    renderWithStore(<Bag />);

    await act(() => add!({ productId: 7, title: 'Oak Table', image: '', company: 'Modenza', color: 'oak', unitPrice: 100, quantity: 1 }));

    await waitFor(() => expect(sent).toEqual([{ kind: 'addedToBag', productId: 7 }]));
  });
});
