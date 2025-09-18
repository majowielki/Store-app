import React from 'react';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";

import { Label } from "./ui/label";

type OptionType = string | { value: string; label: string };
interface SelectInputProps {
  name: string;
  label?: string;
  defaultValue?: string;
  options: OptionType[];
  includeAll?: boolean;
  value?: string;
  onValueChange?: (value: string) => void;
}

const FormSelect = ({ label, name, options, defaultValue, includeAll = false, value, onValueChange }: SelectInputProps) => {
  // Normalize options to array of { value, label }
  const normalizedOptions: { value: string; label: string }[] = [];
  const seen = new Set<string>();
  if (includeAll && !seen.has('all')) {
    seen.add('all');
    normalizedOptions.push({ value: 'all', label: 'All' });
  }
  for (const opt of options) {
    if (!opt) continue;
    let v: string, l: string;
    if (typeof opt === 'string') {
      v = opt;
      l = opt === 'all' ? 'All' : v.charAt(0).toUpperCase() + v.slice(1);
    } else {
      v = opt.value;
      l = opt.label;
    }
    if (!seen.has(v)) {
      seen.add(v);
      normalizedOptions.push({ value: v, label: l });
    }
  }
  const normalizedDefault = defaultValue ? String(defaultValue) : undefined;
  const initial = normalizedDefault && normalizedOptions.some(o => o.value === normalizedDefault)
    ? normalizedDefault
    : (includeAll ? 'all' : normalizedOptions[0]?.value);
  const [internal, setInternal] = React.useState<string>(initial ?? 'all');
  const current = value !== undefined ? value : internal;
  // Keep internal in sync if defaultValue/options change (rare)
  React.useEffect(() => {
    const d = defaultValue ? String(defaultValue) : undefined;
    const nextInitial = d && normalizedOptions.some(o => o.value === d) ? d : (includeAll ? 'all' : normalizedOptions[0]?.value);
    setInternal((prev) => (prev ? prev : nextInitial ?? 'all'));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [defaultValue, includeAll, JSON.stringify(options)]);
  return (
    <div className="mb-2">
      <Label htmlFor={name} className="capitalize">
        {label || name}
      </Label>
      {/* hidden input to participate in native form submission */}
      <input type="hidden" name={name} value={current ?? ''} />
      <Select
        value={current}
        onValueChange={(v) => {
          setInternal(v);
          onValueChange?.(v);
        }}
      >
        <SelectTrigger id={name}>
          {/* Always show 'All' if value is 'all' */}
          <SelectValue>{normalizedOptions.find(o => o.value === current)?.label ?? current}</SelectValue>
        </SelectTrigger>
        <SelectContent>
          {normalizedOptions.map((item) => (
            <SelectItem key={item.value} value={item.value}>
              {item.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
};
export default FormSelect;
