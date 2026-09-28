import { useId, useState, type ReactNode } from 'react';
import ResponsiveImage from '@/components/ResponsiveImage';
import Markdown from '@/components/content/Markdown';
import { fieldLabelClass } from '@/components/FormInput';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { cn } from '@/lib/utils';

/** A labelled field with an optional hint under it. */
export const Field = ({ label, hint, htmlFor, children, className }: { label: string; hint?: ReactNode; htmlFor?: string; children: ReactNode; className?: string }) => (
  <div className={cn('grid gap-2', className)}>
    <Label htmlFor={htmlFor} className={fieldLabelClass}>
      {label}
    </Label>
    {children}
    {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
  </div>
);

interface TextFieldProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  hint?: ReactNode;
  required?: boolean;
  maxLength?: number;
  type?: 'text' | 'url' | 'number' | 'datetime-local';
  multiline?: boolean;
  className?: string;
}

export const TextField = ({ label, value, onChange, hint, required, maxLength, type = 'text', multiline = false, className }: TextFieldProps) => {
  const id = useId();
  return (
    <Field label={label} hint={hint} htmlFor={id} className={className}>
      {multiline ? (
        <Textarea id={id} value={value} onChange={(e) => onChange(e.target.value)} required={required} maxLength={maxLength} className="min-h-[90px]" />
      ) : (
        <Input id={id} type={type} value={value} onChange={(e) => onChange(e.target.value)} required={required} maxLength={maxLength} />
      )}
    </Field>
  );
};

/** Markdown with a preview in the shop's typography; raw HTML is not rendered there either. */
export const MarkdownField = ({ label, value, onChange, required }: { label: string; value: string; onChange: (value: string) => void; required?: boolean }) => {
  const id = useId();
  const [preview, setPreview] = useState(false);
  return (
    <div className="grid gap-2">
      <div className="flex items-center justify-between">
        <Label htmlFor={id} className={fieldLabelClass}>
          {label}
        </Label>
        <div role="tablist" aria-label={`${label} view`} className="flex rounded-full border p-0.5 text-xs">
          {['Write', 'Preview'].map((tab) => {
            const active = (tab === 'Preview') === preview;
            return (
              <button
                key={tab}
                type="button"
                role="tab"
                aria-selected={active}
                onClick={() => setPreview(tab === 'Preview')}
                className={cn('rounded-full px-3 py-1 transition-colors', active ? 'bg-foreground text-background' : 'text-muted-foreground')}
              >
                {tab}
              </button>
            );
          })}
        </div>
      </div>
      {preview ? (
        <div className="min-h-[260px] rounded-lg border bg-card px-6 py-5">
          {value.trim() ? <Markdown>{value}</Markdown> : <p className="text-sm text-muted-foreground">Nothing to preview yet.</p>}
        </div>
      ) : (
        <Textarea id={id} value={value} onChange={(e) => onChange(e.target.value)} required={required} className="min-h-[260px] font-mono text-sm" />
      )}
      <p className="text-xs text-muted-foreground">Markdown: ## for a heading, **bold**, - for a list, [text](/products) for a link.</p>
    </div>
  );
};

/** A picture URL with the picture itself next to it. */
export const ImageField = ({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) => {
  const id = useId();
  return (
    <Field label={label} htmlFor={id} hint="The address of a picture in the product-images container (Blobs/ in the repository).">
      <div className="flex items-center gap-3">
        <Input id={id} type="url" value={value} onChange={(e) => onChange(e.target.value)} required />
        <div className="h-12 w-16 shrink-0 overflow-hidden rounded-md border bg-muted">
          {value && <ResponsiveImage size="thumbnail" src={value} alt="" className="h-full w-full object-cover" />}
        </div>
      </div>
    </Field>
  );
};

/** Published or not: an unpublished entry exists only here. */
export const PublishedField = ({ value, onChange }: { value: boolean; onChange: (value: boolean) => void }) => (
  <label className="flex items-center gap-3 text-sm">
    <input type="checkbox" checked={value} onChange={(e) => onChange(e.target.checked)} className="h-4 w-4 accent-foreground" />
    Published - the shop shows it
  </label>
);
