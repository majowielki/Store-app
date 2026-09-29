import type { LiveStatus } from '@/features/live/liveOrders';
import { useLiveStatus } from '@/features/live/liveStatusContext';
import { cn } from '@/lib/utils';

const LABELS: Record<LiveStatus, string> = {
  connecting: 'Connecting…',
  live: 'Live',
  reconnecting: 'Reconnecting…',
  offline: 'Offline, retrying',
};

const DOTS: Record<LiveStatus, string> = {
  connecting: 'bg-muted-foreground animate-pulse',
  live: 'bg-success',
  reconnecting: 'bg-muted-foreground animate-pulse',
  offline: 'bg-destructive',
};

/** Whether the list on screen follows new orders by itself: the panel's connection to the live feed. */
const LiveStatusIndicator = ({ className }: { className?: string }) => {
  const status = useLiveStatus();
  if (status === null) return null;
  return (
    <span role="status" className={cn('inline-flex items-center gap-2 text-xs text-muted-foreground', className)}>
      <span aria-hidden="true" className={cn('size-2 rounded-full', DOTS[status])} />
      {LABELS[status]}
    </span>
  );
};

export default LiveStatusIndicator;
