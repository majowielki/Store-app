import { useEffect, useState } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { CheckCircle2, X } from 'lucide-react';
import { useCreateReviewMutation } from '@/api/reviews';
import { errorMessage } from '@/api/problem';
import type { Review } from '@/api/types';
import { useAppSelector } from '@/hooks';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { StarsInput } from './Stars';

// The limits the review service checks (ADR 012); the form says so before it is sent
export const BODY_MIN = 20;
export const BODY_MAX = 1000;
export const TITLE_MAX = 80;

interface ReviewDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  productId: number;
  productTitle: string;
  /** A rejected review being written again: the form starts from it. */
  previous?: Review;
}

/** Writing a review of a bought product; it is read by the shop's team before anyone else sees it. */
const ReviewDialog = ({ open, onOpenChange, productId, productTitle, previous }: ReviewDialogProps) => {
  const isDemo = useAppSelector((state) => state.session.user?.isDemo ?? false);
  const [rating, setRating] = useState(previous?.rating ?? 0);
  const [title, setTitle] = useState(previous?.title ?? '');
  const [body, setBody] = useState(previous?.body ?? '');
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);
  const [createReview, { isLoading }] = useCreateReviewMutation();
  const length = body.trim().length;

  // Each opening starts from a clean form (or from the rejected review being written again)
  useEffect(() => {
    if (!open) return;
    setRating(previous?.rating ?? 0);
    setTitle(previous?.title ?? '');
    setBody(previous?.body ?? '');
    setError(null);
    setSent(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (rating === 0) {
      setError('Choose how many stars you give it.');
      return;
    }
    if (length < BODY_MIN) {
      setError(`Tell us a little more - a review has at least ${BODY_MIN} characters.`);
      return;
    }
    setError(null);
    try {
      await createReview({ productId, rating, title: title.trim() || null, body: body.trim() }).unwrap();
      setSent(true);
    } catch (failure) {
      setError(errorMessage(failure));
    }
  };

  const change = (isOpen: boolean) => onOpenChange(isOpen);

  return (
    <Dialog.Root open={open} onOpenChange={change}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/40 backdrop-blur-xs data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 max-h-[90vh] w-[calc(100%-2rem)] max-w-lg -translate-x-1/2 -translate-y-1/2 overflow-y-auto rounded-3xl border bg-background p-6 shadow-2xl data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 sm:p-8">
          <Dialog.Close className="absolute right-4 top-4 grid h-10 w-10 place-items-center rounded-full opacity-70 transition-opacity hover:opacity-100 focus:outline-hidden focus-visible:ring-2 focus-visible:ring-ring">
            <X className="h-5 w-5" />
            <span className="sr-only">Close</span>
          </Dialog.Close>
          <p className="eyebrow">{previous ? 'Write it again' : 'Your review'}</p>
          <Dialog.Title className="display mt-2 pr-10 text-3xl leading-tight">{productTitle}</Dialog.Title>

          {sent ? (
            <div role="status" className="mt-6 grid gap-4">
              <p className="flex items-start gap-3 rounded-2xl bg-secondary/60 p-4 text-sm">
                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" />
                <span>
                  Thank you! Your review is awaiting moderation - it appears on the product page once our team has read it.
                  {isDemo && ' Written from the demo account, it stays visible to you only and is removed after a day.'}
                </span>
              </p>
              <Button onClick={() => change(false)}>Done</Button>
            </div>
          ) : (
            <form onSubmit={submit} className="mt-6 grid gap-5" aria-label={`Review of ${productTitle}`} noValidate>
              <Dialog.Description className="text-sm text-muted-foreground">
                Tell other customers how it looks and feels at home. No links, e-mail addresses or phone numbers - every review is read by
                our team before it appears.
              </Dialog.Description>
              <StarsInput value={rating} onChange={setRating} />
              <div className="grid gap-2">
                <Label htmlFor="review-title">Title (optional)</Label>
                <Input id="review-title" value={title} maxLength={TITLE_MAX} onChange={(e) => setTitle(e.target.value)} placeholder="Solid and warm" />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="review-body">Review</Label>
                <Textarea
                  id="review-body"
                  value={body}
                  maxLength={BODY_MAX}
                  rows={6}
                  onChange={(e) => setBody(e.target.value)}
                  placeholder="What do you like about it? How is it holding up?"
                  aria-describedby="review-body-count"
                />
                <p id="review-body-count" className="text-right text-xs tabular-nums text-muted-foreground">
                  {length < BODY_MIN ? `${BODY_MIN - length} more characters needed` : `${length} / ${BODY_MAX}`}
                </p>
              </div>
              {error && (
                <p role="alert" className="text-sm text-destructive">
                  {error}
                </p>
              )}
              <Button type="submit" size="lg" disabled={isLoading}>
                {isLoading ? 'Sending…' : 'Send review'}
              </Button>
            </form>
          )}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
};

export default ReviewDialog;
