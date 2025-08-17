import React from 'react';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";

import { Label } from "./ui/label";

interface SelectInputProps {
  name: string;
  label?: string;
  defaultValue?: string;
  options: string[];
  includeAll?: boolean;
  value?: string;
  onValueChange?: (value: string) => void;
};

const FormSelect = ({ label, name, options, defaultValue, includeAll = false, value, onValueChange }: SelectInputProps) => {
  // Build a unique, ordered options list and prevent duplicate 'all'
  const seen = new Set<string>();
  const opts: string[] = [];

  if (includeAll) {
    seen.add('all');
    opts.push('all');
  }

  for (const opt of options) {
    if (!opt) continue;
    if (opt === 'all') {
      // Skip if 'all' already included via includeAll
      if (!includeAll && !seen.has('all')) {
        seen.add('all');
        opts.push('all');
      }
      continue;
    }
    if (!seen.has(opt)) {
      seen.add(opt);
      opts.push(opt);
    }
  }

  const normalizedDefault = defaultValue ? String(defaultValue) : undefined;
  const initial = normalizedDefault && opts.includes(normalizedDefault)
    ? normalizedDefault
    : (includeAll ? 'all' : opts[0]);
  const [internal, setInternal] = React.useState<string>(initial ?? 'all');
  const current = value !== undefined ? value : internal;

  // Keep internal in sync if defaultValue/options change (rare)
  React.useEffect(() => {
    const d = defaultValue ? String(defaultValue) : undefined;
    const nextInitial = d && opts.includes(d) ? d : (includeAll ? 'all' : opts[0]);
    setInternal((prev) => (prev ? prev : nextInitial ?? 'all'));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [defaultValue, includeAll, options.join('|')]);
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
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {opts.map((item) => {
            return (
              <SelectItem key={item} value={item}>
                {item === 'all' ? 'All' : item}
              </SelectItem>
            );
          })}
        </SelectContent>
      </Select>
    </div>
  );
}
export default FormSelect;
