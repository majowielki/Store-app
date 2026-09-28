import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import * as Dialog from '@radix-ui/react-dialog';
import { Check, Flag, X } from 'lucide-react';
import { useGetAdminReviewsQuery, useModerateReviewsMutation } from '@/api/reviews';
import type { AdminReview } from '@/api/types';
import { Roles } from '@/features/session/roles';
import { useAppSelector } from '@/hooks';
import { useProductsById } from '@/hooks/use-products-by-id';
import { toast } from '@/hooks/use-toast';
import { cn } from '@/lib/utils';
import { formatDateTime } from '@/utils';
import PageNumbers from '@/components/PageNumbers';
import Stars from '@/components/reviews/Stars';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';

const PAGE_SIZE = 20;

const tabs = [
  { value: 'queue', label: 'To moderate' },
  { value: 'reported', label: 'Reported' },
  { value: 'published', label: 'Published' },
  { value: 'rejected', label: 'Rejected' },
  { value: 'all', label: 'All' },
] as const;

/** Reasons the administrator picks most often; the author of the review sees the one sent. */
const quickReasons = ['Not about the product', 'Offensive language', 'Advertising or spam', 'Personal data'];

const statusBadge: Record<AdminReview['status'], { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
  pending: { label: 'Pending', variant: 'secondary' },
  published: { label: 'Published', variant: 'default' },
  rejected: { label: 'Rejected', variant: 'destructive' },
};

/** Asks why the reviews are rejected; the reason is sent to their authors. */
const RejectDialog = ({ count, busy, onReject, onCancel }: { count: number; busy: boolean; onReject: (reason: string) => void; onCancel: () => void }) => {
  const [reason, setReason] = useState('');
  return (
    <Dialog.Root open={count > 0} onOpenChange={(open) => !open && onCancel()}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/80 data-[state=open]:animate-in data-[state=open]:fade-in-0" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-[calc(100%-2rem)] max-w-md -translate-x-1/2 -translate-y-1/2 rounded-lg border bg-background p-6 shadow-lg">
          <Dialog.Title className="text-lg font-semibold">Reject {count === 1 ? 'the review' : `${count} reviews`}</Dialog.Title>
          <Dialog.Description className="mt-2 text-sm text-muted-foreground">Its author sees the reason and may write the review again.</Dialog.Description>
          <form
            className="mt-4 grid gap-3"
            onSubmit={(event) => {
              event.preventDefault();
              if (reason.trim()) onReject(reason.trim());
            }}
          >
            <div className="flex flex-wrap gap-2">
              {quickReasons.map((quick) => (
                <button key={quick} type="button" onClick={() => setReason(quick)} className="rounded-full border px-3 py-1 text-xs hover:border-foreground">
                  {quick}
                </button>
              ))}
            </div>
            <Label htmlFor="reject-reason">Reason</Label>
            <Textarea id="reject-reason" value={reason} maxLength={300} rows={3} onChange={(e) => setReason(e.target.value)} required />
            <div className="mt-2 flex justify-end gap-2">
              <Button type="button" variant="outline" onClick={onCancel} disabled={busy}>
                Cancel
              </Button>
              <Button type="submit" variant="destructive" disabled={busy || !reason.trim()}>
                Reject
              </Button>
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
};

/** The moderation queue of the reviews (ADR 012): only the true administrator decides, the demo one looks. */
const Reviews = () => {
  const isDemoAdmin = useAppSelector((state) => state.session.user?.roles.includes(Roles.DemoAdmin) ?? false);
  const [status, setStatus] = useState<(typeof tabs)[number]['value']>('queue');
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<string[]>([]);
  const [rejecting, setRejecting] = useState<string[]>([]);
  const { data, isLoading } = useGetAdminReviewsQuery({ status, page, pageSize: PAGE_SIZE });
  const [moderate, { isLoading: moderating }] = useModerateReviewsMutation();

  const items = useMemo(() => data?.items ?? [], [data]);
  const productIds = useMemo(() => [...new Set(items.map((review) => review.productId).filter((id) => id > 0))], [items]);
  const { products } = useProductsById(productIds);
  const titles = new Map(products.map((product) => [product.id, product.title]));
  const allSelected = items.length > 0 && items.every((review) => selected.includes(review.id));

  const show = (value: (typeof tabs)[number]['value']) => {
    setStatus(value);
    setPage(1);
    setSelected([]);
  };

  const decide = async (ids: string[], decision: 'approve' | 'reject', reason?: string) => {
    try {
      const result = await moderate({ ids, decision, reason: reason ?? null }).unwrap();
      toast({ description: `${result.moderated} ${result.moderated === 1 ? 'review' : 'reviews'} ${decision === 'approve' ? 'published' : 'rejected'}` });
      setSelected((current) => current.filter((id) => !ids.includes(id)));
      setRejecting([]);
    } catch {
      // Reported by the error middleware (the demo administrator's 403 included)
    }
  };

  const toggle = (id: string, checked: boolean) =>
    setSelected((current) => (checked ? [...current, id] : current.filter((selectedId) => selectedId !== id)));

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-xl font-semibold">Reviews</h2>
        {data && (
          <p className="text-sm text-muted-foreground">
            {data.totalCount} {data.totalCount === 1 ? 'review' : 'reviews'}
          </p>
        )}
      </div>
      {isDemoAdmin && (
        <p className="rounded-lg border bg-muted/50 p-3 text-sm text-muted-foreground">
          As the demo administrator you see the queue but cannot decide. Texts nobody has approved yet and the customers' accounts stay hidden.
        </p>
      )}

      <div className="flex flex-wrap gap-2" role="tablist" aria-label="Reviews by state">
        {tabs.map((tab) => (
          <button
            key={tab.value}
            type="button"
            role="tab"
            aria-selected={status === tab.value}
            onClick={() => show(tab.value)}
            className={cn(
              'inline-flex h-9 items-center rounded-full border px-4 text-sm transition-colors',
              status === tab.value ? 'border-foreground bg-foreground text-background' : 'hover:border-foreground',
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <Card className="p-2">
        <div className="flex flex-wrap items-center gap-3 border-b px-3 py-3">
          <Checkbox
            id="reviews-select-all"
            checked={allSelected}
            onCheckedChange={(checked) => setSelected(checked === true ? items.map((review) => review.id) : [])}
            aria-label="Select every review on this page"
          />
          <span className="text-sm text-muted-foreground">{selected.length > 0 ? `${selected.length} selected` : 'Select reviews to decide on many at once'}</span>
          {selected.length > 0 && (
            <div className="ml-auto flex gap-2">
              <Button size="sm" onClick={() => decide(selected, 'approve')} disabled={moderating}>
                <Check />
                Approve selected
              </Button>
              <Button size="sm" variant="destructive" onClick={() => setRejecting(selected)} disabled={moderating}>
                <X />
                Reject selected
              </Button>
            </div>
          )}
        </div>

        {isLoading ? (
          <div className="p-6">Loading...</div>
        ) : items.length === 0 ? (
          <p className="p-6 text-sm text-muted-foreground">{status === 'queue' ? 'Nothing waits for a decision.' : 'No reviews here.'}</p>
        ) : (
          <ul className="divide-y">
            {items.map((review) => {
              const badge = statusBadge[review.status];
              const product = titles.get(review.productId) ?? review.productSlug ?? `Product ${review.productId}`;
              return (
                <li key={review.id} className="grid grid-cols-[auto_1fr] gap-3 px-3 py-4" aria-label={`Review of ${product} by ${review.authorName}`}>
                  <Checkbox
                    checked={selected.includes(review.id)}
                    onCheckedChange={(checked) => toggle(review.id, checked === true)}
                    aria-label={`Select the review of ${product} by ${review.authorName}`}
                    className="mt-1"
                  />
                  <div className="grid gap-2">
                    <div className="flex flex-wrap items-center gap-2 text-sm">
                      {review.productId > 0 ? (
                        <Link to={`/products/${review.productId}`} className="font-medium hover:underline">
                          {product}
                        </Link>
                      ) : (
                        <span className="font-medium">{product}</span>
                      )}
                      <Stars rating={review.rating} size="xs" />
                      <Badge variant={badge.variant}>{badge.label}</Badge>
                      {review.reported && (
                        <Badge variant="outline" className="gap-1">
                          <Flag className="h-3 w-3" />
                          Reported {review.reportCount > 1 ? `×${review.reportCount}` : ''}
                        </Badge>
                      )}
                      {review.source === 'seed' && <Badge variant="outline">Seeded</Badge>}
                      {review.demo && <Badge variant="outline">Demo session</Badge>}
                    </div>
                    {review.title && <p className="font-medium">{review.title}</p>}
                    <p className="text-sm text-muted-foreground">{review.body}</p>
                    {review.reportReasons.length > 0 && (
                      <p className="text-xs text-muted-foreground">Reports: {review.reportReasons.join(' · ')}</p>
                    )}
                    {review.rejectionReason && <p className="text-xs text-destructive">Rejected: {review.rejectionReason}</p>}
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <p className="text-xs text-muted-foreground">
                        {review.authorName} · sent {formatDateTime(review.submittedAt)}
                        {review.moderatedAt && ` · decided ${formatDateTime(review.moderatedAt)}`}
                      </p>
                      <div className="flex gap-2">
                        {(review.status !== 'published' || review.reported) && (
                          <Button size="sm" variant="outline" onClick={() => decide([review.id], 'approve')} disabled={moderating} aria-label={`Approve the review by ${review.authorName}`}>
                            <Check />
                            Approve
                          </Button>
                        )}
                        {review.status !== 'rejected' && (
                          <Button size="sm" variant="outline" onClick={() => setRejecting([review.id])} disabled={moderating} aria-label={`Reject the review by ${review.authorName}`}>
                            <X />
                            Reject
                          </Button>
                        )}
                      </div>
                    </div>
                  </div>
                </li>
              );
            })}
          </ul>
        )}
        <PageNumbers
          page={page}
          totalPages={data?.totalPages ?? 1}
          onPageChange={(next) => {
            setPage(next);
            setSelected([]);
          }}
        />
      </Card>

      <RejectDialog
        key={rejecting.join(',')}
        count={rejecting.length}
        busy={moderating}
        onReject={(reason) => decide(rejecting, 'reject', reason)}
        onCancel={() => setRejecting([])}
      />
    </div>
  );
};

export default Reviews;
