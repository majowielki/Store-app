import { Check } from 'lucide-react';
import { useFinishes } from '@/hooks/use-finishes';
import { cn } from '@/lib/utils';
import { fieldLabelClass } from './FormInput';
import { isLightSwatch } from './swatch/contrast';
import Swatch from './swatch/Swatch';

interface SelectProductColorProps {
  colors: string[];
  productColor: string;
  setProductColor: React.Dispatch<React.SetStateAction<string>>;
};

/** The colours a product is sold in, as swatches the customer picks one of. */
const SelectProductColor = ({
  colors,
  productColor,
  setProductColor,
}: SelectProductColorProps) => {
  const { finishOf, nameOf } = useFinishes();

  return (
    <div>
      <p className={fieldLabelClass}>
        Colour — <span className="normal-case tracking-normal text-foreground">{nameOf(productColor)}</span>
      </p>
      <div className="mt-3 flex flex-wrap gap-2.5" role="radiogroup" aria-label="Colour">
        {colors.map((color) => {
          const selected = color === productColor;
          return (
            <button
              key={color}
              type="button"
              role="radio"
              aria-checked={selected}
              aria-label={nameOf(color)}
              title={nameOf(color)}
              onClick={() => setProductColor(color)}
              className={cn(
                'relative grid h-9 w-9 place-items-center rounded-full ring-offset-2 ring-offset-background transition-all duration-300',
                selected ? 'ring-2 ring-foreground' : 'hover:scale-110',
              )}
            >
              <Swatch color={color} className="absolute inset-0 h-full w-full" />
              {selected && (
                <Check
                  className={cn(
                    'relative h-4 w-4 drop-shadow-sm',
                    isLightSwatch(finishOf(color)?.swatch) ? 'text-black' : 'text-white',
                  )}
                />
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
}
export default SelectProductColor;
