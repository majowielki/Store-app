import { useState } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { useReportReviewMutation } from '@/api/reviews';
import { errorMessage } from '@/api/problem';
import { useAppSelector } from '@/hooks';
import { toast } from '@/hooks/use-toast';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { REVIEW_REASON_MAX } from './rating';

interface ReportDialogProps {
  reviewId: string | null;
  onClose: () => void;
}

/** Reporting a review: from a customer's account it is hidden until the shop's team has looked at it. */
const ReportDialog = ({ reviewId, onClose }: ReportDialogProps) => {
  const isDemo = useAppSelector((state) => state.session.user?.isDemo ?? false);
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [report, { isLoading }] = useReportReviewMutation();

  const close = () => {
    setReason('');
    setError(null);
    onClose();
  };

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!reviewId) return;
    try {
      await report({ id: reviewId, reason: reason.trim() }).unwrap();
      toast({
        description: isDemo
          ? 'Thank you. The review is hidden for you; reports from the demo account do not hide it from others.'
          : 'Thank you. The review is hidden until our team has looked at it.',
      });
      close();
    } catch (failure) {
      setError(errorMessage(failure));
    }
  };

  return (
    <Dialog.Root open={reviewId !== null} onOpenChange={(open) => !open && close()}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/40 backdrop-blur-xs data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-[calc(100%-2rem)] max-w-md -translate-x-1/2 -translate-y-1/2 rounded-3xl border bg-background p-6 shadow-2xl data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0">
          <Dialog.Title className="display text-2xl">Report this review</Dialog.Title>
          <Dialog.Description className="mt-2 text-sm text-muted-foreground">
            Is it offensive, off-topic or advertising something? Tell us and our team will take a look.
          </Dialog.Description>
          <form onSubmit={submit} className="mt-5 grid gap-4" aria-label="Report this review">
            <div className="grid gap-2">
              <Label htmlFor="report-reason">What is wrong with it? (optional)</Label>
              <Textarea id="report-reason" value={reason} maxLength={REVIEW_REASON_MAX} rows={3} onChange={(e) => setReason(e.target.value)} />
            </div>
            {error && (
              <p role="alert" className="text-sm text-destructive">
                {error}
              </p>
            )}
            <div className="flex justify-end gap-2">
              <Button type="button" variant="outline" onClick={close} disabled={isLoading}>
                Cancel
              </Button>
              <Button type="submit" disabled={isLoading}>
                Report review
              </Button>
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
};

export default ReportDialog;
