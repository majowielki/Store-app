import { useState } from 'react';
import { useSubscribeToNewsletterMutation } from '@/api/newsletter';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { toast } from '@/hooks/use-toast';

const NewsletterSection = () => {
  const [email, setEmail] = useState('');
  const [subscribe, { isLoading }] = useSubscribeToNewsletterMutation();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      toast({ variant: 'destructive', description: 'Please enter a valid email address' });
      return;
    }
    try {
      await subscribe(email).unwrap();
      toast({ description: 'Thanks for subscribing!' });
      setEmail('');
    } catch {
      // Reported by the error middleware
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
            <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Your email" aria-label="Email address" />
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Sending...' : 'Subscribe'}
            </Button>
          </form>
        </div>
      </div>
    </section>
  );
};

export default NewsletterSection;
