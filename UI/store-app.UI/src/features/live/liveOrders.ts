import { HttpTransportType, HubConnectionBuilder, LogLevel, type HubConnection, type IRetryPolicy } from '@microsoft/signalr';
import { freshAccessToken } from '@/api/baseQuery';
import type { OrderStatus } from '@/api/types';
import { apiBaseUrl } from '@/config';

/** Where the order service maps the hub (LiveOrdersHub.Path), under the API's base address. */
export const LIVE_ORDERS_PATH = '/admin/orders/live';

/** The one message the hub sends (ILiveOrdersClient.OrderChanged). */
export const ORDER_CHANGED = 'OrderChanged';

/** One change of an order as the hub tells it (LiveOrderUpdate): which order, and the status it entered. */
export interface LiveOrderUpdate {
  orderId: number;
  status: OrderStatus;
  at: string;
}

/** Where the panel's connection to the feed stands. */
export type LiveStatus = 'connecting' | 'live' | 'reconnecting' | 'offline';

/** The waits between attempts to connect again; the last one repeats for as long as the panel is open. */
export const RECONNECT_DELAYS_MS = [0, 2_000, 10_000, 30_000];

export const reconnectDelay = (attempt: number): number => RECONNECT_DELAYS_MS[Math.min(attempt, RECONNECT_DELAYS_MS.length - 1)];

const keepReconnecting: IRetryPolicy = {
  nextRetryDelayInMilliseconds: ({ previousRetryCount }) => reconnectDelay(previousRetryCount),
};

/**
 * A connection to the feed: one WebSocket straight to the hub, the only transport the server
 * takes, without a negotiate request. It opens with a token good for a while yet; the server
 * closes it when that token expires, and the reconnect brings a new one.
 */
export const createLiveOrdersConnection = (): HubConnection =>
  new HubConnectionBuilder()
    .withUrl(`${apiBaseUrl}${LIVE_ORDERS_PATH}`, {
      transport: HttpTransportType.WebSockets,
      skipNegotiation: true,
      accessTokenFactory: async () => (await freshAccessToken()) ?? '',
    })
    .withAutomaticReconnect(keepReconnecting)
    .configureLogging(LogLevel.Warning)
    .build();

/** What the panel says about a change, when it says anything: a new order, and one paid for. */
export const describeUpdate = ({ orderId, status }: LiveOrderUpdate): string | null => {
  switch (status) {
    case 'Placed':
      return `New order #${orderId}.`;
    case 'Paid':
      return `Order #${orderId} is paid.`;
    default:
      return null;
  }
};
