import { formatAsDollars } from "@/utils";
import { useEffect, useState } from "react";

import { Label } from "@/components/ui/label";
import { Slider } from "./ui/slider";

interface FormRangeProps {
  name: string; // expected to be "price"
  label?: string;
  defaultValue?: string; // "min,max" or "min-max"
};

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
    <div className="mb-2">
      <Label htmlFor={name} className="capitalize flex justify-between">
        {label || name}
        <span>
          {formatAsDollars(range[0])} - {formatAsDollars(range[1])}
        </span>
      </Label>

      <div className="mt-4">
        <Slider
          id={name}
          step={step}
          min={min}
          max={max}
          value={range}
          onValueChange={(value) => setRange([value[0], value[1] ?? value[0]])}
        />
      </div>
      <div className="mt-3 flex items-center gap-3">
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
          className="w-24 rounded-md border border-input bg-background px-2 py-1 text-sm"
          aria-label="Minimum price"
        />
        <span className="text-sm text-muted-foreground">to</span>
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
          className="w-24 rounded-md border border-input bg-background px-2 py-1 text-sm"
          aria-label="Maximum price"
        />
      </div>

      <input type="hidden" name={name} value={hidden} />
    </div>
  );
}
export default FormRange;
