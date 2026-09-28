import { useState } from 'react';
import { PenLine } from 'lucide-react';
import type { Review } from '@/api/types';
import { Button } from '@/components/ui/button';
import ReviewDialog from './ReviewDialog';

const states: Record<string, string> = {
  pending: 'Awaiting moderation',
  published: 'Reviewed',
  rejected: 'Not published',
};

interface ReviewLineActionProps {
  productId: number;
  productTitle: string;
  /** The customer's review of the product, if they wrote one. */
  review?: Review;
}

/** On a line of a paid order: "Write a review", or where the customer's review of it stands. */
const ReviewLineAction = ({ productId, productTitle, review }: ReviewLineActionProps) => {
  const [open, setOpen] = useState(false);
  const canWrite = !review || review.status === 'rejected';

  return (
    <>
      {review && <span className="text-xs text-muted-foreground">{review.reported ? 'Hidden after a report' : states[review.status]}</span>}
      {canWrite && (
        <Button variant="outline" size="sm" onClick={() => setOpen(true)} aria-label={`${review ? 'Write again' : 'Write a review'}: ${productTitle}`}>
          <PenLine />
          {review ? 'Write again' : 'Write a review'}
        </Button>
      )}
      <ReviewDialog open={open} onOpenChange={setOpen} productId={productId} productTitle={productTitle} previous={review?.status === 'rejected' ? review : undefined} />
    </>
  );
};

export default ReviewLineAction;
