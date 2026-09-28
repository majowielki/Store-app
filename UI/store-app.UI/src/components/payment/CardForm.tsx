import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { CreditCard, Lock } from 'lucide-react';
import { useAuthenticatePaymentMutation, useConfirmPaymentMutation, useGetTestCardsQuery, useStartPaymentMutation } from '@/api/payments';
import { errorMessage, HttpStatus, isApiError } from '@/api/problem';
import type { Order, Payment } from '@/api/types';
import { fieldLabelClass } from '@/components/FormInput';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { formatAsDollars } from '@/utils';
import {
  chargeOutcomes,
  CVC_MAX_DIGITS,
  declineMessages,
  formatCardNumber,
  formatCvc,
  formatExpiry,
  parseExpiry,
  SAMPLE_CVC,
  sampleExpiry,
} from './cards';
import SecureCheckDialog from './SecureCheckDialog';

interface CardFormProps {
  order: Order;
  /** The card went through; the shop hears it a moment later by webhook. */
  onPaid: () => void;
  /** The payment can no longer be made here (paid meanwhile, cancelled): read the order again. */
  onClosed: () => void;
}

/**
 * The card form of the payment page. It opens the order's payment (the same one on every visit),
 * sends the card straight to the payment service and answers what comes back: paid, a 3-D Secure
 * window, or a refusal shown next to the form so the customer can try another card.
 */
const CardForm = ({ order, onPaid, onClosed }: CardFormProps) => {
  const [startPayment, start] = useStartPaymentMutation();
  const { data: testCards = [] } = useGetTestCardsQuery();
  const [confirmPayment, confirm] = useConfirmPaymentMutation();
  const [authenticatePayment, authentication] = useAuthenticatePaymentMutation();
  const [cardNumber, setCardNumber] = useState('');
  const [expiry, setExpiry] = useState('');
  const [cvc, setCvc] = useState('');
  const [problem, setProblem] = useState<string | null>(null);
  const [challenge, setChallenge] = useState<Payment | null>(null);

  useEffect(() => {
    void startPayment(order.id);
  }, [order.id, startPayment]);

  const payment = start.data;

  const settle = (result: Payment) => {
    if (result.status === 'succeeded') onPaid();
    else if (result.status === 'requiresAction') setChallenge(result);
    else if (result.status === 'requiresPaymentMethod') setProblem(declineMessages[result.declineReason ?? ''] ?? 'The card was refused. Try another one.');
    else onClosed();
  };

  const fail = (error: unknown) => {
    setProblem(errorMessage(error));
    if (isApiError(error) && error.status === HttpStatus.Conflict) onClosed();
  };

  const pay = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!payment) return;
    setProblem(null);
    const date = parseExpiry(expiry);
    if (!date) {
      setProblem('Give the expiry date as MM / YY.');
      return;
    }
    try {
      settle(await confirmPayment({ paymentId: payment.paymentId, card: { cardNumber, expMonth: date.month, expYear: date.year, cvc } }).unwrap());
    } catch (error) {
      fail(error);
    }
  };

  const answer = async (approve: boolean) => {
    if (!payment) return;
    try {
      const result = await authenticatePayment({ paymentId: payment.paymentId, approve }).unwrap();
      setChallenge(null);
      settle(result);
    } catch (error) {
      setChallenge(null);
      fail(error);
    }
  };

  const fillTestCard = (number: string) => {
    setCardNumber(number);
    setExpiry(sampleExpiry());
    setCvc(SAMPLE_CVC);
    setProblem(null);
  };

  if (start.isError) {
    return (
      <div role="alert" className="grid gap-4">
        <p className="text-sm">{errorMessage(start.error)}</p>
        <Button asChild variant="outline" className="justify-self-start">
          <Link to={`/orders/${order.id}`}>See your order</Link>
        </Button>
      </div>
    );
  }

  if (!payment) {
    return (
      <div aria-busy="true" aria-label="Opening the payment" className="grid gap-4">
        <Skeleton className="h-12 w-full rounded-full" />
        <Skeleton className="h-12 w-full rounded-full" />
        <Skeleton className="h-12 w-full rounded-full" />
      </div>
    );
  }

  return (
    <>
      <form onSubmit={pay} aria-label="Card details" className="grid gap-5">
        <div className="grid gap-2">
          <Label htmlFor="card-number" className={fieldLabelClass}>
            Card number
          </Label>
          <div className="relative">
            <Input
              id="card-number"
              inputMode="numeric"
              autoComplete="off"
              placeholder="1234 1234 1234 1234"
              value={cardNumber}
              onChange={(e) => setCardNumber(formatCardNumber(e.target.value))}
              required
              className="h-12 rounded-full pl-5 pr-12 tabular-nums"
            />
            <CreditCard className="pointer-events-none absolute right-5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden />
          </div>
        </div>
        <div className="grid grid-cols-2 gap-4 [&>*]:min-w-0">
          <div className="grid gap-2">
            <Label htmlFor="card-expiry" className={fieldLabelClass}>
              Expiry (MM / YY)
            </Label>
            <Input
              id="card-expiry"
              inputMode="numeric"
              autoComplete="off"
              placeholder="MM / YY"
              value={expiry}
              onChange={(e) => setExpiry(formatExpiry(e.target.value))}
              required
              className="h-12 min-w-0 rounded-full px-5 tabular-nums"
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="card-cvc" className={fieldLabelClass}>
              Security code
            </Label>
            <Input
              id="card-cvc"
              inputMode="numeric"
              autoComplete="off"
              placeholder={SAMPLE_CVC}
              maxLength={CVC_MAX_DIGITS}
              value={cvc}
              onChange={(e) => setCvc(formatCvc(e.target.value))}
              required
              className="h-12 min-w-0 rounded-full px-5 tabular-nums"
            />
          </div>
        </div>
        {problem && (
          <p role="alert" className="rounded-2xl border border-destructive/40 bg-destructive/5 p-4 text-sm text-destructive">
            {problem}
          </p>
        )}
        <Button type="submit" size="lg" className="h-12" disabled={confirm.isLoading}>
          <Lock />
          {confirm.isLoading ? 'Paying...' : `Pay ${formatAsDollars(payment.amount)}`}
        </Button>
      </form>

      <section aria-labelledby="test-cards" className="mt-8 rounded-2xl bg-secondary/60 p-5">
        <h2 id="test-cards" className="eyebrow">
          Test cards
        </h2>
        <p className="mt-2 text-sm text-muted-foreground">This is a demo shop: nothing is charged, and only these cards are taken.</p>
        <ul className="mt-4 grid gap-2">
          {testCards.map((card) => (
            <li key={card.number} className="flex items-center justify-between gap-3 text-sm">
              <span>
                <span className="font-medium tabular-nums">{card.number}</span> <span className="text-muted-foreground">- {chargeOutcomes[card.outcome]}</span>
              </span>
              <Button type="button" size="sm" variant="outline" aria-label={`Use test card ${card.number}`} onClick={() => fillTestCard(card.number)}>
                Use
              </Button>
            </li>
          ))}
        </ul>
      </section>

      <SecureCheckDialog
        open={challenge !== null}
        amount={payment.amount}
        cardBrand={challenge?.cardBrand}
        cardLast4={challenge?.cardLast4}
        busy={authentication.isLoading}
        onAnswer={answer}
      />
    </>
  );
};

export default CardForm;
