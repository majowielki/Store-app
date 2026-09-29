import { act, screen, waitFor } from '@testing-library/react';
import { http } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useGetAdminOrdersQuery } from '@/api/orders';
import { admin, order } from '@/test/fixtures';
import { api, json, page } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import { ORDER_CHANGED, RECONNECT_DELAYS_MS, type LiveOrderUpdate } from './liveOrders';
import { useLiveOrders } from './useLiveOrders';

type Listener = (...args: unknown[]) => void;

/** Plays the SignalR connection: the test says when it starts, fails, drops and what it hears. */
class FakeConnection {
  private readonly listeners = new Map<string, Listener>();
  reconnecting: Listener = () => {};
  reconnected: Listener = () => {};
  closed: Listener = () => {};
  start = vi.fn(async () => {});
  stop = vi.fn(async () => {});

  on(name: string, listener: Listener) {
    this.listeners.set(name, listener);
  }
  onreconnecting(listener: Listener) {
    this.reconnecting = listener;
  }
  onreconnected(listener: Listener) {
    this.reconnected = listener;
  }
  onclose(listener: Listener) {
    this.closed = listener;
  }
  hear(update: LiveOrderUpdate) {
    this.listeners.get(ORDER_CHANGED)?.(update);
  }
}

let connection: FakeConnection;

vi.mock('./liveOrders', async (importOriginal) => ({
  ...(await importOriginal<typeof import('./liveOrders')>()),
  createLiveOrdersConnection: () => connection,
}));

/** The admin orders list with the panel's connection above it. */
const Panel = () => {
  const status = useLiveOrders();
  const { data } = useGetAdminOrdersQuery({ page: 1 });
  return (
    <>
      <p>{status}</p>
      <p>{data ? `${data.items.length} orders` : 'loading'}</p>
    </>
  );
};

describe('useLiveOrders', () => {
  let listed: number;

  beforeEach(() => {
    connection = new FakeConnection();
    listed = 0;
    server.use(
      http.get(api('/admin/orders'), () => {
        listed += 1;
        return json(page(Array.from({ length: listed }, (_, i) => order({ id: i + 1 }))));
      }),
    );
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('reads the orders again and announces a new order when the feed tells of one', async () => {
    renderWithStore(<Panel />, { user: admin });
    expect(await screen.findByText('live')).toBeInTheDocument();
    expect(await screen.findByText('1 orders')).toBeInTheDocument();

    act(() => connection.hear({ orderId: 2, status: 'Placed', at: '2026-09-29T10:00:00Z' }));

    expect(await screen.findByText('2 orders')).toBeInTheDocument();
    expect(screen.getByText('New order #2.')).toBeInTheDocument();
  });

  it('announces a paid order but only reads the list again for other changes', async () => {
    renderWithStore(<Panel />, { user: admin });
    await screen.findByText('1 orders');

    act(() => connection.hear({ orderId: 1, status: 'AwaitingPayment', at: '2026-09-29T10:00:00Z' }));
    await screen.findByText('2 orders');
    act(() => connection.hear({ orderId: 1, status: 'Paid', at: '2026-09-29T10:01:00Z' }));

    expect(await screen.findByText('Order #1 is paid.')).toBeInTheDocument();
    expect(screen.queryByText(/awaiting/i)).not.toBeInTheDocument();
  });

  it('reads the orders again after a reconnect, having missed what happened meanwhile', async () => {
    renderWithStore(<Panel />, { user: admin });
    await screen.findByText('1 orders');

    act(() => connection.reconnecting());
    expect(screen.getByText('reconnecting')).toBeInTheDocument();
    act(() => connection.reconnected());

    expect(await screen.findByText('2 orders')).toBeInTheDocument();
    expect(screen.getByText('live')).toBeInTheDocument();
  });

  it('keeps trying when the feed cannot be reached', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    connection.start.mockRejectedValueOnce(new Error('unreachable'));
    renderWithStore(<Panel />, { user: admin });
    expect(await screen.findByText('offline')).toBeInTheDocument();

    await act(() => vi.advanceTimersByTimeAsync(RECONNECT_DELAYS_MS[1]));

    await waitFor(() => expect(connection.start).toHaveBeenCalledTimes(2));
    expect(await screen.findByText('live')).toBeInTheDocument();
  });

  it('closes the connection when the panel is left', async () => {
    const { unmount } = renderWithStore(<Panel />, { user: admin });
    await screen.findByText('live');

    unmount();

    expect(connection.stop).toHaveBeenCalled();
  });
});
