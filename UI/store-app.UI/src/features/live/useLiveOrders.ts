import { useEffect, useState } from 'react';
import { api } from '@/api/api';
import { useAppDispatch } from '@/hooks';
import { toast } from '@/hooks/use-toast';
import { createLiveOrdersConnection, describeUpdate, ORDER_CHANGED, reconnectDelay, RECONNECT_DELAYS_MS, type LiveOrderUpdate, type LiveStatus } from './liveOrders';

/**
 * Keeps the admin panel connected to the live feed of orders while it is open. Every change makes
 * the orders and the statistics stale, so the page on screen reads them again; a new order and a
 * paid one are also announced. Returns where the connection stands.
 */
export const useLiveOrders = (): LiveStatus => {
  const dispatch = useAppDispatch();
  const [status, setStatus] = useState<LiveStatus>('connecting');

  useEffect(() => {
    const connection = createLiveOrdersConnection();
    let stopped = false;
    let retry: number | undefined;
    const readAgain = () => dispatch(api.util.invalidateTags(['Orders', 'Stats']));

    const start = async (attempt = 0): Promise<void> => {
      try {
        await connection.start();
        if (!stopped) setStatus('live');
      } catch {
        if (stopped) return;
        setStatus('offline');
        retry = window.setTimeout(() => void start(attempt + 1), reconnectDelay(attempt + 1));
      }
    };

    connection.on(ORDER_CHANGED, (update: LiveOrderUpdate) => {
      readAgain();
      const message = describeUpdate(update);
      if (message) toast({ description: message });
    });
    connection.onreconnecting(() => setStatus('reconnecting'));
    // Nothing was told while the connection was down: whatever is on screen is read again
    connection.onreconnected(() => {
      setStatus('live');
      readAgain();
    });
    // The automatic reconnect never gives up, so this is a close it was not asked to recover from
    connection.onclose(() => {
      if (stopped) return;
      setStatus('offline');
      retry = window.setTimeout(() => void start(1), RECONNECT_DELAYS_MS[1]);
    });

    void start();
    return () => {
      stopped = true;
      window.clearTimeout(retry);
      void connection.stop();
    };
  }, [dispatch]);

  return status;
};
