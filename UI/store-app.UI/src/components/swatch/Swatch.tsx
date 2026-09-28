import type { CSSProperties } from 'react';
import type { SwatchPart } from '@/api/types';
import { useFinishes } from '@/hooks/use-finishes';
import { cn } from '@/lib/utils';
import { swatchTextures } from './textures';

/** The second part of a swatch covers its lower right half, split along the diagonal. */
const SECOND_HALF = 'polygon(100% 0, 100% 100%, 0 100%)';

/** A part's surface over its colour, which shows while the picture loads. */
const fill = (part: SwatchPart): CSSProperties => ({
  backgroundColor: part.color,
  backgroundImage: part.texture ? `url(${swatchTextures[part.texture]})` : undefined,
  backgroundSize: 'cover',
  backgroundPosition: 'center',
});

interface SwatchProps {
  /** A product colour: the key of a finish, or a plain colour an older cart or order kept. */
  color: string;
  /** The size, and anything else the place it sits in needs. */
  className?: string;
}

/**
 * A product colour the way its pictures show it: the colour or the surface (a wood, a weave, a
 * stone, a metal) of its finish, split along the diagonal for a product of two. A colour the shop
 * does not know as a finish is drawn as the CSS colour of that name, over the muted background
 * when it names none. Decorative: the name goes next to it or into the control it sits in.
 */
const Swatch = ({ color, className }: SwatchProps) => {
  const { finishOf } = useFinishes();
  const [main, second] = finishOf(color)?.swatch ?? [];

  return (
    <span
      aria-hidden
      className={cn('relative inline-block shrink-0 overflow-hidden rounded-full border border-foreground/15 bg-muted', className)}
      style={main ? fill(main) : { backgroundColor: color }}
    >
      {second && <span className="absolute inset-0" style={{ ...fill(second), clipPath: SECOND_HALF }} />}
    </span>
  );
};

export default Swatch;
