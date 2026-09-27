import { formatAsDollars } from "@/utils";
import { useEffect, useState } from "react";

import { Label } from "@/components/ui/label";
import { Slider } from "./ui/slider";
import { fieldLabelClass } from "./FormInput";

interface FormRangeProps {
  name: string; // expected to be "price"
  label?: string;
  defaultValue?: string; // "min,max" or "min-max"
};

const numberClass =
  "h-9 w-full min-w-0 rounded-lg border border-input bg-card px-3 text-sm tabular-nums transition-[border-color,box-shadow] focus-visible:border-foreground focus-visible:outline-hidden focus-visible:ring-4 focus-visible:ring-foreground/10";

const FormRange = ({ name, label, defaultValue }: FormRangeProps) => {
  const step = 1;
  const min = 0;
  const max = 2000;
  const parse = (val?: string): [number, number] => {
    if (!val) return [min, max];
    const norm = val.replace('-', ',');
    const [a, b] = norm.split(',').map((n) => Number(n));
    const lo = Number.isFinite(a) ? Math.max(min, Math.min(max, a)) : min;
    const hi = Number.isFinite(b) ? Math.max(min, Math.min(max, b)) : max;
    return lo <= hi ? [lo, hi] : [hi, lo];
  };
  const [range, setRange] = useState<[number, number]>(parse(defaultValue));

  // Keep hidden input in sync for form submission (min,max)
  const [hidden, setHidden] = useState<string>(`${range[0]},${range[1]}`);
  useEffect(() => {
    setHidden(`${range[0]},${range[1]}`);
  }, [range]);

  return (
    <div className="grid gap-2">
      <Label htmlFor={name} className="flex items-baseline justify-between">
        <span className={fieldLabelClass}>{label || name}</span>
        <span className="text-xs tabular-nums text-foreground">
          {formatAsDollars(range[0])} – {formatAsDollars(range[1])}
        </span>
      </Label>

      <div className="py-3">
        <Slider
          id={name}
          step={step}
          min={min}
          max={max}
          value={range}
          onValueChange={(value) => setRange([value[0], value[1] ?? value[0]])}
        />
      </div>
      <div className="flex items-center gap-2">
        <input
          type="number"
          min={min}
          max={max}
          step={step}
          value={range[0]}
          onChange={(e) => {
            const v = Number(e.target.value);
            const lo = Math.min(Math.max(min, v), range[1]);
            setRange([lo, range[1]]);
          }}
          className={numberClass}
          aria-label="Minimum price"
        />
        <span className="text-muted-foreground">–</span>
        <input
          type="number"
          min={min}
          max={max}
          step={step}
          value={range[1]}
          onChange={(e) => {
            const v = Number(e.target.value);
            const hi = Math.max(Math.min(max, v), range[0]);
            setRange([range[0], hi]);
          }}
          className={numberClass}
          aria-label="Maximum price"
        />
      </div>

      <input type="hidden" name={name} value={hidden} />
    </div>
  );
}
export default FormRange;
