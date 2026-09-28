import { useState } from 'react';
import { BellRing, Check } from 'lucide-react';
import { useNotifyWhenBackMutation } from '@/api/catalog';
import { errorMessage, HttpStatus, isApiError } from '@/api/problem';
import { useAppSelector } from '@/hooks';
import { cn } from '@/lib/utils';
import { Button } from './ui/button';
import { Input } from './ui/input';

/** The longest address the catalogue keeps for an alert. */
const EMAIL_MAX_LENGTH = 256;

/**
 * In place of "Add to bag" on a sold-out product: an e-mail address to write to once the
 * product is back. A signed-in customer finds their own address filled in.
 */
const NotifyWhenBack = ({ productId, className }: { productId: number; className?: string }) => {
  const account = useAppSelector((state) => state.session.user?.email ?? '');
  const [email, setEmail] = useState(account);
  const [notifyWhenBack, { isLoading }] = useNotifyWhenBackMutation();
  const [done, setDone] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    try {
      await notifyWhenBack({ id: productId, email: email.trim() }).unwrap();
      setDone(email.trim());
    } catch (failure) {
      setError(isApiError(failure) && failure.status === HttpStatus.Conflict ? 'It is back in stock - reload the page to add it to your bag.' : errorMessage(failure));
    }
  };

  if (done) {
    return (
      <p role="status" className={cn('flex items-center gap-2 rounded-2xl bg-secondary/60 p-4 text-sm', className)}>
        <Check className="h-4 w-4 shrink-0" />
        We will write to {done} as soon as it is back.
      </p>
    );
  }

  return (
    <form onSubmit={submit} className={cn('grid gap-2', className)} aria-label="Notify me when it is back">
      <p className="text-sm text-muted-foreground">Sold out for now. Leave your e-mail and we will tell you when it is back.</p>
      <div className="flex gap-2">
        <Input
          type="email"
          required
          maxLength={EMAIL_MAX_LENGTH}
          aria-label="E-mail address"
          placeholder="you@example.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="h-12 rounded-full px-5"
        />
        <Button type="submit" size="lg" className="h-12 shrink-0" disabled={isLoading}>
          <BellRing />
          Notify me
        </Button>
      </div>
      {error && (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      )}
    </form>
  );
};

export default NotifyWhenBack;
