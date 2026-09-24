import { cn } from '@/lib/utils';

interface SaleBadgeProps {
  percent: number;
  className?: string;
}

/** The discount as a small terracotta pill, e.g. "-20%". */
const SaleBadge = ({ percent, className }: SaleBadgeProps) => {
  if (!Number.isFinite(percent) || percent <= 0) return null;
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full bg-brand px-2.5 py-1 text-[11px] font-semibold leading-none tracking-wide text-brand-foreground',
        className,
      )}
    >
      -{Math.round(percent)}%
    </span>
  );
};

export default SaleBadge;
