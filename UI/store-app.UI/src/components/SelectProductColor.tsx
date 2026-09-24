import { Check } from 'lucide-react';
import { cn } from '@/lib/utils';

interface SelectProductColorProps {
  colors: string[];
  productColor: string;
  setProductColor: React.Dispatch<React.SetStateAction<string>>;
};

// Light swatches get a dark tick, the rest a light one
const lightColors = new Set(['white', 'yellow', 'silver', 'gold', 'beige', 'pink']);

const SelectProductColor = ({
  colors,
  productColor,
  setProductColor,
}: SelectProductColorProps) => {
  return (
    <div>
      <h4 className="text-[0.7rem] font-medium uppercase tracking-[0.14em] text-muted-foreground">
        Colour — <span className="capitalize text-foreground">{productColor}</span>
      </h4>
      <div className="mt-3 flex flex-wrap gap-2.5" role="radiogroup" aria-label="Colour">
        {colors.map((color) => {
          const selected = color === productColor;
          return (
            <button
              key={color}
              type="button"
              role="radio"
              aria-checked={selected}
              aria-label={color}
              onClick={() => setProductColor(color)}
              className={cn(
                'grid h-9 w-9 place-items-center rounded-full border border-foreground/15 ring-offset-2 ring-offset-background transition-all duration-300',
                selected ? 'ring-2 ring-foreground' : 'hover:scale-110',
              )}
              style={{ backgroundColor: color }}
            >
              {selected && <Check className={cn('h-4 w-4', lightColors.has(color) ? 'text-black' : 'text-white')} />}
            </button>
          );
        })}
      </div>
    </div>
  );
}
export default SelectProductColor;
