import { Check } from 'lucide-react';
import { cn } from '@/lib/utils';

const steps = ['Cart', 'Delivery', 'Payment', 'Done'];

/** Where the customer is in the purchase: the steps behind them ticked, the current one outlined. */
const CheckoutSteps = ({ current, className }: { current: number; className?: string }) => (
  <ol aria-label="Checkout steps" className={cn('flex flex-wrap items-center gap-3 text-xs', className)}>
    {steps.map((step, index) => (
      <li key={step} aria-current={index === current ? 'step' : undefined} className="flex items-center gap-3">
        <span
          className={cn(
            'grid h-6 w-6 place-items-center rounded-full border text-[10px] font-medium',
            index < current && 'border-foreground bg-foreground text-background',
            index === current && 'border-foreground',
            index > current && 'text-muted-foreground',
          )}
        >
          {index < current ? <Check className="h-3 w-3" /> : index + 1}
        </span>
        <span className={index > current ? 'text-muted-foreground' : 'font-medium'}>{step}</span>
        {index < steps.length - 1 && <span className="h-px w-8 bg-border" />}
      </li>
    ))}
  </ol>
);

export default CheckoutSteps;
