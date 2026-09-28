import { useEffect, useState } from 'react';
import { Timer } from 'lucide-react';
import { cn } from '@/lib/utils';

const pad = (value: number) => String(value).padStart(2, '0');

/**
 * The time left to pay, counted down every second from the order's deadline. When it runs out
 * the page is told, so it can read the order again (the shop cancels it).
 */
const PaymentCountdown = ({ dueAt, onExpired, className }: { dueAt: string; onExpired?: () => void; className?: string }) => {
  const due = new Date(dueAt).getTime();
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);

  const left = Math.max(0, Math.ceil((due - now) / 1000));
  const expired = left === 0;

  useEffect(() => {
    if (expired) onExpired?.();
  }, [expired, onExpired]);

  return (
    <p role="timer" className={cn('flex items-center gap-2 text-sm', expired ? 'text-destructive' : 'text-muted-foreground', className)}>
      <Timer className="h-4 w-4 shrink-0" aria-hidden />
      {expired ? (
        'The time to pay has run out.'
      ) : (
        <span>
          Your pieces are held for <span className="font-medium tabular-nums text-foreground">{pad(Math.floor(left / 60))}:{pad(left % 60)}</span>
        </span>
      )}
    </p>
  );
};

export default PaymentCountdown;
