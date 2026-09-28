import { useFinishes } from '@/hooks/use-finishes';
import { cn } from '@/lib/utils';
import Swatch from './Swatch';

/** A product colour in a line of text - a cart line, an order line: its swatch and its name. */
const FinishLabel = ({ color, className }: { color: string; className?: string }) => {
  const { nameOf } = useFinishes();

  return (
    <span className={cn('inline-flex items-center gap-1.5 align-middle', className)}>
      <Swatch color={color} className="h-3.5 w-3.5" />
      {nameOf(color)}
    </span>
  );
};

export default FinishLabel;
