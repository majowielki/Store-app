import { useId, useState } from 'react';
import { Tag, X } from 'lucide-react';
import { codeApplied, codeRemoved } from '@/features/cart/discountCodeSlice';
import { useDiscountCode } from '@/features/cart/useDiscountCode';
import { useAppDispatch } from '@/hooks';
import { cn } from '@/lib/utils';
import { formatAsDollars } from '@/utils';
import { Button } from './ui/button';
import { Input } from './ui/input';

/**
 * The discount code field of the cart. An accepted code shows what it takes off and can be
 * removed; a refused one stays in the field with the order service's reason under it.
 */
const DiscountCodeField = ({ className }: { className?: string }) => {
  const dispatch = useAppDispatch();
  const { code, check, refusal, isChecking } = useDiscountCode();
  const [draft, setDraft] = useState(code ?? '');
  const id = useId();

  if (code && check) {
    return (
      <div className={cn('flex items-center justify-between gap-3 rounded-xl border border-dashed px-4 py-3 text-sm', className)}>
        <span className="flex items-center gap-2">
          <Tag className="h-4 w-4 text-success" aria-hidden />
          <span>
            <span className="font-medium">{check.code}</span>{' '}
            <span className="text-muted-foreground">
              {check.kind === 'Percent' ? `${check.value}% off` : `${formatAsDollars(check.value)} off`}
            </span>
          </span>
        </span>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          aria-label={`Remove code ${check.code}`}
          onClick={() => {
            setDraft('');
            dispatch(codeRemoved());
          }}
        >
          <X className="h-4 w-4" />
        </Button>
      </div>
    );
  }

  return (
    <form
      className={className}
      onSubmit={(event) => {
        event.preventDefault();
        dispatch(codeApplied(draft));
      }}
    >
      <label htmlFor={id} className="text-[0.7rem] font-medium uppercase tracking-[0.14em] text-muted-foreground">
        Discount code
      </label>
      <div className="mt-2 flex gap-2">
        <Input
          id={id}
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          autoComplete="off"
          spellCheck={false}
          className="uppercase placeholder:normal-case"
          placeholder="Enter a code"
          aria-invalid={refusal ? true : undefined}
          aria-describedby={refusal ? `${id}-refusal` : undefined}
        />
        <Button type="submit" variant="outline" disabled={draft.trim().length === 0 || isChecking}>
          {isChecking ? 'Checking…' : 'Apply'}
        </Button>
      </div>
      {refusal && (
        <p id={`${id}-refusal`} role="alert" className="mt-2 text-xs text-destructive">
          {refusal}
        </p>
      )}
    </form>
  );
};

export default DiscountCodeField;
