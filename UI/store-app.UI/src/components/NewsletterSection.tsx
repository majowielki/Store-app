import { useState } from 'react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { useToast } from '@/hooks/use-toast';

export type SubscribeFn = (email: string) => Promise<void>;

type Props = { onSubscribe?: SubscribeFn };

const NewsletterSection = ({ onSubscribe }: Props) => {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const { toast } = useToast();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
  toast({ variant: 'destructive', description: 'Please enter a valid email address' });
      return;
    }
    try {
      setLoading(true);
      if (onSubscribe) {
        await onSubscribe(email);
      }
  toast({ description: 'Thanks for subscribing!' });
      setEmail('');
  } catch {
  toast({ variant: 'destructive', description: 'Subscription failed' });
    } finally {
      setLoading(false);
    }
  };

  return (
  <section className="py-12 bg-muted/30">
      <div className="align-element">
        <div className="grid gap-4 md:grid-cols-2 items-center">
          <div>
            <h3 className="text-xl font-semibold">Join our newsletter</h3>
            <p className="text-muted-foreground mt-2">Get updates about promotions and new arrivals.</p>
          </div>
          <form className="flex gap-2" onSubmit={handleSubmit}>
            <Input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Your email"
              aria-label="Email address"
            />
            <Button type="submit" disabled={loading}>{loading ? 'Sending...' : 'Subscribe'}</Button>
          </form>
        </div>
      </div>
    </section>
  );
};

export default NewsletterSection;
