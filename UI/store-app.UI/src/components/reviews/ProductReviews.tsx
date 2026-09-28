import { useState } from 'react';
import { Link } from 'react-router-dom';
import { BadgeCheck, Flag, PenLine, X } from 'lucide-react';
import { useGetMyProductReviewQuery, useGetReviewSummaryQuery, useGetReviewsQuery } from '@/api/reviews';
import type { MyProductReview, Review, ReviewBlockReason, ReviewSort, ReviewSummary } from '@/api/types';
import { useAppSelector } from '@/hooks';
import { cn } from '@/lib/utils';
import { formatDate } from '@/utils';
import Reveal from '@/components/Reveal';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import ReportDialog from './ReportDialog';
import ReviewDialog from './ReviewDialog';
import { formatRating, ratingScale, starsLabel } from './rating';
import Stars from './Stars';

const PAGE = 5;

const sorts: { value: ReviewSort; label: string }[] = [
  { value: 'newest', label: 'Newest' },
  { value: 'highest', label: 'Highest rated' },
  { value: 'lowest', label: 'Lowest rated' },
  { value: 'oldest', label: 'Oldest' },
];

/** The bars of the summary, the most stars first. */
const barsFromTop = [...ratingScale].reverse();

const pill = 'inline-flex h-9 items-center gap-1.5 whitespace-nowrap rounded-full border px-4 text-sm transition-colors';

/** One review as the shop prints it; others' reviews can be reported by a signed-in customer. */
const ReviewCard = ({ review, onReport }: { review: Review; onReport?: (id: string) => void }) => (
  <article className="grid gap-3 border-b py-7 first:pt-0" aria-label={`Review by ${review.authorName}`}>
    <div className="flex flex-wrap items-center justify-between gap-3">
      <Stars rating={review.rating} />
      <time dateTime={review.createdAt} className="text-xs text-muted-foreground">
        {formatDate(review.createdAt)}
      </time>
    </div>
    {review.title && <h3 className="font-medium leading-snug">{review.title}</h3>}
    <p className="leading-relaxed text-muted-foreground">{review.body}</p>
    <div className="flex flex-wrap items-center justify-between gap-3 text-sm">
      <p className="flex items-center gap-2">
        <span className="font-medium">{review.authorName}</span>
        {review.verifiedPurchase && (
          <span className="inline-flex items-center gap-1 text-xs text-success">
            <BadgeCheck className="h-3.5 w-3.5" />
            Verified purchase
          </span>
        )}
      </p>
      {onReport && (
        <button
          type="button"
          onClick={() => onReport(review.id)}
          className="inline-flex items-center gap-1.5 text-xs text-muted-foreground transition-colors hover:text-foreground"
          aria-label={`Report the review by ${review.authorName}`}
        >
          <Flag className="h-3.5 w-3.5" />
          Report
        </button>
      )}
    </div>
  </article>
);

/** The average, the stars and a bar per number of stars; a bar filters the list to its reviews. */
const Summary = ({ summary, rating, onRating }: { summary: ReviewSummary; rating?: number; onRating: (stars?: number) => void }) => (
  <div>
    <p className="display text-6xl tabular-nums">{formatRating(summary.averageRating)}</p>
    <Stars rating={summary.averageRating} size="lg" className="mt-3" />
    <p className="mt-2 text-sm text-muted-foreground">
      Based on {summary.reviewCount} {summary.reviewCount === 1 ? 'review' : 'reviews'}
    </p>
    <ul className="mt-6 grid gap-1.5">
      {barsFromTop.map((stars) => {
        const count = summary.distribution[String(stars)] ?? 0;
        const share = summary.reviewCount > 0 ? (count / summary.reviewCount) * 100 : 0;
        const active = rating === stars;
        return (
          <li key={stars}>
            <button
              type="button"
              disabled={count === 0}
              aria-pressed={active}
              onClick={() => onRating(active ? undefined : stars)}
              className={cn(
                'grid w-full grid-cols-[3.5rem_1fr_2rem] items-center gap-3 rounded-lg px-2 py-1 text-sm transition-colors enabled:hover:bg-secondary/70 disabled:opacity-50',
                active && 'bg-secondary',
              )}
              aria-label={`${starsLabel(stars)}: ${count} ${count === 1 ? 'review' : 'reviews'}`}
            >
              <span className="text-left tabular-nums">{starsLabel(stars)}</span>
              <span className="h-1.5 overflow-hidden rounded-full bg-foreground/10">
                <span className="block h-full rounded-full bg-foreground transition-[width] duration-500" style={{ width: `${share}%` }} />
              </span>
              <span className="text-right tabular-nums text-muted-foreground">{count}</span>
            </button>
          </li>
        );
      })}
    </ul>
  </div>
);

const reasons: Record<ReviewBlockReason, string> = {
  notPurchased: 'Reviews come from customers who bought the piece - yours is welcome once it is yours.',
  alreadyReviewed: 'Thank you for reviewing this piece.',
  dailyLimit: 'You have written as many reviews as a day allows - you can review this one tomorrow.',
  signInAgain: 'Sign in again to write a review.',
};

/** What the visitor can do: sign in, write a review, or why they cannot write one. */
const WritePrompt = ({ mine, signedIn, onWrite }: { mine?: MyProductReview; signedIn: boolean; onWrite: () => void }) => {
  if (!signedIn) {
    return (
      <div className="grid gap-3 rounded-2xl bg-secondary/60 p-5 text-sm">
        <p>Bought this piece? Sign in to tell others what you think.</p>
        <Button asChild variant="outline" className="justify-self-start">
          <Link to="/login">Sign in to review</Link>
        </Button>
      </div>
    );
  }
  if (!mine) return null;
  if (mine.canReview) {
    return (
      <Button onClick={onWrite} className="justify-self-start">
        <PenLine />
        {mine.review ? 'Write it again' : 'Write a review'}
      </Button>
    );
  }
  return <p className="rounded-2xl bg-secondary/60 p-5 text-sm">{mine.reason ? reasons[mine.reason] : 'You cannot review this piece right now.'}</p>;
};

/** The visitor's own review while others do not see it: waiting, not published, or hidden after a report. */
const OwnReview = ({ review, isDemo }: { review: Review; isDemo: boolean }) => {
  const note =
    review.status === 'pending'
      ? 'Awaiting moderation - only you can see it until our team has read it.'
      : review.status === 'rejected'
        ? `Not published${review.rejectionReason ? `: ${review.rejectionReason}` : ''}. You can write it again.`
        : 'Hidden from others while our team looks at a report.';
  return (
    <div className="mb-8 rounded-2xl border border-dashed p-5">
      <p className="eyebrow">Your review</p>
      <p role="status" className="mt-2 text-sm">
        {note}
        {isDemo && ' Written from the demo account, it is removed after a day.'}
      </p>
      <div className="mt-4">
        <ReviewCard review={review} />
      </div>
    </div>
  );
};

/** The reviews section of a product page (ADR 012). */
const ProductReviews = ({ productId, productTitle }: { productId: number; productTitle: string }) => {
  const user = useAppSelector((state) => state.session.user);
  const [rating, setRating] = useState<number | undefined>();
  const [verified, setVerified] = useState(false);
  const [sort, setSort] = useState<ReviewSort>('newest');
  const [shown, setShown] = useState(PAGE);
  const [writing, setWriting] = useState(false);
  const [reporting, setReporting] = useState<string | null>(null);

  const { data: summary } = useGetReviewSummaryQuery(productId);
  const { data: reviews, isFetching } = useGetReviewsQuery({
    productId,
    rating,
    verified: verified || undefined,
    sort,
    pageSize: shown,
  });
  const { data: mine } = useGetMyProductReviewQuery(productId, { skip: !user });

  const own = mine?.review;
  const ownApart = own && (own.status !== 'published' || own.reported);
  const items = (reviews?.items ?? []).filter((review) => review.id !== own?.id || !ownApart);
  const filtered = rating !== undefined || verified;

  const filterBy = (stars?: number) => {
    setRating(stars);
    setShown(PAGE);
  };

  return (
    <section id="reviews" className="mt-24 scroll-mt-24 border-t pt-16 md:mt-32" aria-labelledby="reviews-heading">
      <Reveal as="header">
        <p className="eyebrow">Reviews</p>
        <h2 id="reviews-heading" className="display mt-3 text-4xl md:text-5xl">
          What customers say
        </h2>
      </Reveal>

      <div className="mt-10 grid gap-12 lg:grid-cols-12 lg:gap-16">
        <aside className="grid content-start gap-8 lg:col-span-4">
          {summary && summary.reviewCount > 0 ? (
            <Summary summary={summary} rating={rating} onRating={filterBy} />
          ) : (
            <p className="text-muted-foreground">No reviews yet - be the first to share what you think.</p>
          )}
          <WritePrompt mine={mine} signedIn={Boolean(user)} onWrite={() => setWriting(true)} />
        </aside>

        <div className="lg:col-span-8">
          <div className="mb-8 flex flex-wrap items-center gap-2" role="toolbar" aria-label="Order and filter the reviews">
            {sorts.map((option) => (
              <button
                key={option.value}
                type="button"
                aria-pressed={sort === option.value}
                onClick={() => setSort(option.value)}
                className={cn(pill, sort === option.value ? 'border-foreground bg-foreground text-background' : 'hover:border-foreground')}
              >
                {option.label}
              </button>
            ))}
            {rating !== undefined && (
              <button type="button" onClick={() => filterBy(undefined)} className={cn(pill, 'border-brand text-brand hover:bg-brand/10')}>
                {starsLabel(rating)} only
                <X className="h-3.5 w-3.5" />
                <span className="sr-only">(clear)</span>
              </button>
            )}
            <div className="ml-auto flex items-center gap-2">
              <Checkbox
                id="reviews-verified"
                checked={verified}
                onCheckedChange={(checked) => {
                  setVerified(checked === true);
                  setShown(PAGE);
                }}
              />
              <Label htmlFor="reviews-verified" className="text-sm font-normal">
                Verified purchases only
              </Label>
            </div>
          </div>

          {own && ownApart && <OwnReview review={own} isDemo={user?.isDemo ?? false} />}

          {items.length > 0 ? (
            <div aria-busy={isFetching}>
              {items.map((review) => (
                <ReviewCard key={review.id} review={review} onReport={user && review.id !== own?.id ? setReporting : undefined} />
              ))}
            </div>
          ) : (
            reviews && <p className="text-muted-foreground">{filtered ? 'No reviews match these filters.' : 'Nobody has reviewed it yet.'}</p>
          )}

          {reviews && reviews.totalCount > reviews.items.length && (
            <Button variant="outline" className="mt-8" onClick={() => setShown((count) => count + PAGE)} disabled={isFetching}>
              Show more reviews
            </Button>
          )}
        </div>
      </div>

      {user && (
        <ReviewDialog
          open={writing}
          onOpenChange={setWriting}
          productId={productId}
          productTitle={productTitle}
          previous={own?.status === 'rejected' ? own : undefined}
        />
      )}
      <ReportDialog reviewId={reporting} onClose={() => setReporting(null)} />
    </section>
  );
};

export default ProductReviews;
