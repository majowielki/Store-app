import { createContext, useContext } from 'react';
import type { LiveStatus } from './liveOrders';

/** Where the admin panel's connection to the live feed of orders stands; null outside the panel. */
export const LiveStatusContext = createContext<LiveStatus | null>(null);

export const useLiveStatus = (): LiveStatus | null => useContext(LiveStatusContext);
