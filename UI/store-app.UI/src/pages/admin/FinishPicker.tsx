import type { Finish } from '@/api/types';
import Swatch from '@/components/swatch/Swatch';
import { cn } from '@/lib/utils';

interface FinishPickerProps {
  finishes: Finish[];
  /** The keys the product is sold in, in order. */
  chosen: string[];
  onToggle: (key: string) => void;
  className?: string;
}

/** Every finish as a swatch: a click adds it to the product's colours, or takes it off. */
const FinishPicker = ({ finishes, chosen, onToggle, className }: FinishPickerProps) => {
  if (finishes.length === 0) return null;

  return (
    <div role="group" aria-label="finishes" className={cn('flex flex-wrap gap-1.5', className)}>
      {finishes.map((finish) => {
        const on = chosen.includes(finish.key);
        return (
          <button
            key={finish.key}
            type="button"
            aria-pressed={on}
            aria-label={finish.name}
            title={`${finish.name} (${finish.key})`}
            onClick={() => onToggle(finish.key)}
            className={cn(
              'rounded-full ring-offset-2 ring-offset-background transition-transform duration-200',
              on ? 'ring-2 ring-foreground' : 'hover:scale-110',
            )}
          >
            <Swatch color={finish.key} className="block h-7 w-7" />
          </button>
        );
      })}
    </div>
  );
};

export default FinishPicker;
