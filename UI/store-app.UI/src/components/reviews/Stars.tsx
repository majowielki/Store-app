import { useState } from 'react';
import { Star } from 'lucide-react';
import { cn } from '@/lib/utils';
import { formatRating, RATING_MAX, RATING_MIN, ratingScale, starsLabel } from './rating';

const sizes = {
  xs: 'h-3 w-3',
  sm: 'h-3.5 w-3.5',
  md: 'h-4 w-4',
  lg: 'h-5 w-5',
} as const;

interface StarsProps {
  rating: number;
  size?: keyof typeof sizes;
  className?: string;
}

/** Five stars filled up to the rating (a fraction fills part of a star), read out as "Rated 4.5 out of 5". */
const Stars = ({ rating, size = 'sm', className }: StarsProps) => (
  <span role="img" aria-label={`Rated ${formatRating(rating)} out of ${RATING_MAX}`} className={cn('inline-flex items-center gap-0.5', className)}>
    {ratingScale.map((position) => {
      const fill = Math.min(Math.max(rating - (position - 1), 0), 1);
      return (
        <span key={position} className="relative inline-block">
          <Star aria-hidden className={cn(sizes[size], 'text-foreground/20')} strokeWidth={1.5} />
          {fill > 0 && (
            <span className="absolute inset-0 overflow-hidden" style={{ width: `${fill * 100}%` }}>
              <Star aria-hidden className={cn(sizes[size], 'fill-current text-foreground')} strokeWidth={1.5} />
            </span>
          )}
        </span>
      );
    })}
  </span>
);

/** The rating of a product next to its title on a card: stars, the average and the number of reviews; nothing without reviews. */
export const RatingLine = ({ average, count, className }: { average: number; count: number; className?: string }) => {
  if (count <= 0) return null;
  return (
    <p className={cn('flex items-center gap-1.5 text-xs text-muted-foreground', className)}>
      <Stars rating={average} size="xs" />
      <span className="tabular-nums text-foreground">{formatRating(average)}</span>
      <span className="tabular-nums">({count})</span>
    </p>
  );
};

/** What each number of stars says, from one up. */
const labels = ['Poor', 'Fair', 'Good', 'Very good', 'Excellent'];

interface StarsInputProps {
  value: number;
  onChange: (value: number) => void;
  name?: string;
}

/** Picking the stars: a radio group, so arrows and the screen reader work as with any other. */
export const StarsInput = ({ value, onChange, name = 'rating' }: StarsInputProps) => {
  const [hovered, setHovered] = useState(0);
  const shown = hovered || value;

  return (
    <fieldset className="grid gap-2">
      <legend className="text-[0.7rem] font-medium uppercase tracking-[0.14em] text-muted-foreground">Your rating</legend>
      <div className="flex items-center gap-3">
        <div className="flex" onMouseLeave={() => setHovered(0)}>
          {ratingScale.map((stars) => (
            <label key={stars} className="relative cursor-pointer p-0.5" onMouseEnter={() => setHovered(stars)}>
              <input
                type="radio"
                name={name}
                value={stars}
                checked={value === stars}
                onChange={() => onChange(stars)}
                className="peer absolute inset-0 z-10 m-0 cursor-pointer appearance-none opacity-0"
                aria-label={starsLabel(stars)}
              />
              <Star
                aria-hidden
                strokeWidth={1.5}
                className={cn(
                  'pointer-events-none h-7 w-7 rounded-sm transition-transform duration-200 peer-focus-visible:ring-2 peer-focus-visible:ring-ring',
                  stars <= shown ? 'fill-current text-foreground' : 'text-foreground/25',
                  stars === hovered && 'scale-110',
                )}
              />
            </label>
          ))}
        </div>
        <span className="text-sm text-muted-foreground" aria-live="polite">
          {shown ? labels[shown - RATING_MIN] : `Choose ${RATING_MIN} to ${RATING_MAX} stars`}
        </span>
      </div>
    </fieldset>
  );
};

export default Stars;
