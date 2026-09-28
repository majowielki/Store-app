import * as Dialog from '@radix-ui/react-dialog';
import { ShieldCheck } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { formatAsDollars } from '@/utils';
import { describeCard } from './cards';

interface SecureCheckDialogProps {
  open: boolean;
  amount: number;
  cardBrand?: string | null;
  cardLast4?: string | null;
  busy: boolean;
  onAnswer: (approve: boolean) => void;
}

/**
 * The 3-D Secure window a bank shows for some cards, played by the demo: the customer approves or
 * rejects the payment. It cannot be dismissed any other way - the bank waits for an answer.
 */
const SecureCheckDialog = ({ open, amount, cardBrand, cardLast4, busy, onAnswer }: SecureCheckDialogProps) => (
  <Dialog.Root open={open}>
    <Dialog.Portal>
      <Dialog.Overlay className="fixed inset-0 z-50 bg-black/60 backdrop-blur-xs data-[state=open]:animate-in data-[state=open]:fade-in-0" />
      <Dialog.Content
        onEscapeKeyDown={(event) => event.preventDefault()}
        onPointerDownOutside={(event) => event.preventDefault()}
        className="fixed left-1/2 top-1/2 z-50 w-[calc(100%-2rem)] max-w-sm -translate-x-1/2 -translate-y-1/2 overflow-hidden rounded-2xl border bg-background shadow-2xl data-[state=open]:animate-in data-[state=open]:fade-in-0 data-[state=open]:zoom-in-95"
      >
        <div className="flex items-center gap-2 bg-foreground px-5 py-3 text-background">
          <ShieldCheck className="h-4 w-4" aria-hidden />
          <span className="text-xs font-medium uppercase tracking-[0.14em]">Secure check · your bank</span>
        </div>
        <div className="p-6">
          <Dialog.Title className="display text-2xl">Is this you?</Dialog.Title>
          <Dialog.Description className="mt-3 text-sm leading-relaxed text-muted-foreground">
            Approve a payment of <span className="font-medium text-foreground">{formatAsDollars(amount)}</span> to Store with{' '}
            {describeCard(cardBrand, cardLast4)}. In a real shop your bank would ask for a code or its app here.
          </Dialog.Description>
          <div className="mt-6 grid grid-cols-2 gap-2">
            <Button type="button" variant="outline" disabled={busy} onClick={() => onAnswer(false)}>
              Reject
            </Button>
            <Button type="button" disabled={busy} onClick={() => onAnswer(true)}>
              Approve
            </Button>
          </div>
        </div>
      </Dialog.Content>
    </Dialog.Portal>
  </Dialog.Root>
);

export default SecureCheckDialog;
