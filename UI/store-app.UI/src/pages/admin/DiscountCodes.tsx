import { useState } from 'react';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import { useDeleteDiscountCodeMutation, useGetDiscountCodesQuery, useSaveDiscountCodeMutation } from '@/api/orders';
import ConfirmDialog from '@/components/ConfirmDialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { toast } from '@/hooks/use-toast';
import { formatAsDollars, formatDate, type DiscountCode, type DiscountCodePayload } from '@/utils';
import { Field, TextField } from './content/fields';

/** The form's values: every field as typed, dates as the datetime-local input gives them. */
interface Draft {
  code: string;
  kind: 'Percent' | 'Amount';
  value: string;
  minimumSubtotal: string;
  startsAt: string;
  expiresAt: string;
  usageLimit: string;
  isActive: boolean;
}

const emptyDraft: Draft = { code: '', kind: 'Percent', value: '', minimumSubtotal: '', startsAt: '', expiresAt: '', usageLimit: '', isActive: true };

/** "2026-12-31T23:59" in the admin's own time, from a UTC timestamp. */
const toLocalInput = (value?: string | null) => {
  if (!value) return '';
  const date = new Date(value);
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
};

const toDraft = (code: DiscountCode): Draft => ({
  code: code.code,
  kind: code.kind === 'Amount' ? 'Amount' : 'Percent',
  value: String(code.value),
  minimumSubtotal: code.minimumSubtotal ? String(code.minimumSubtotal) : '',
  startsAt: toLocalInput(code.startsAt),
  expiresAt: toLocalInput(code.expiresAt),
  usageLimit: code.usageLimit ? String(code.usageLimit) : '',
  isActive: code.isActive,
});

const toPayload = (draft: Draft): DiscountCodePayload => ({
  code: draft.code.trim(),
  kind: draft.kind,
  value: Number(draft.value),
  minimumSubtotal: draft.minimumSubtotal ? Number(draft.minimumSubtotal) : null,
  startsAt: draft.startsAt ? new Date(draft.startsAt).toISOString() : null,
  expiresAt: draft.expiresAt ? new Date(draft.expiresAt).toISOString() : null,
  usageLimit: draft.usageLimit ? Number(draft.usageLimit) : null,
  isActive: draft.isActive,
});

const describeDiscount = (code: DiscountCode) => (code.kind === 'Percent' ? `${code.value}%` : formatAsDollars(code.value));

/** Whether the code works today, in one word for the list. */
const state = (code: DiscountCode, now = Date.now()) => {
  if (!code.isActive) return { label: 'Off', variant: 'outline' as const };
  if (code.expiresAt && new Date(code.expiresAt).getTime() <= now) return { label: 'Expired', variant: 'secondary' as const };
  if (code.startsAt && new Date(code.startsAt).getTime() > now) return { label: 'Scheduled', variant: 'secondary' as const };
  if (code.usageLimit && code.timesUsed >= code.usageLimit) return { label: 'Used up', variant: 'secondary' as const };
  return { label: 'Active', variant: 'default' as const };
};

/**
 * Discount codes: the list with each code's rules and uses, a side panel to add or change
 * one, and deleting a code no order used (a used one is switched off instead).
 */
const DiscountCodes = () => {
  const { data: codes, isLoading } = useGetDiscountCodesQuery();
  const [saveCode, { isLoading: saving }] = useSaveDiscountCodeMutation();
  const [deleteCode, { isLoading: deleting }] = useDeleteDiscountCodeMutation();
  const [editing, setEditing] = useState<{ id?: number; draft: Draft } | null>(null);
  const [toDelete, setToDelete] = useState<DiscountCode | null>(null);

  const set = <K extends keyof Draft>(key: K, value: Draft[K]) => setEditing((current) => (current ? { ...current, draft: { ...current.draft, [key]: value } } : current));

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!editing) return;
    try {
      const saved = await saveCode({ id: editing.id, body: toPayload(editing.draft) }).unwrap();
      toast({ description: `${saved.code} is saved.` });
      setEditing(null);
    } catch {
      // reported by the error middleware
    }
  };

  const confirmDelete = async () => {
    if (!toDelete) return;
    try {
      await deleteCode(toDelete.id).unwrap();
      toast({ description: `${toDelete.code} deleted.` });
    } catch {
      // reported by the error middleware
    } finally {
      setToDelete(null);
    }
  };

  const draft = editing?.draft;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        <h2 className="text-xl font-semibold">Discount codes</h2>
        <Button type="button" onClick={() => setEditing({ draft: emptyDraft })}>
          <Plus />
          New code
        </Button>
      </div>
      <Card className="p-2">
        {isLoading ? (
          <div className="p-6">Loading...</div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Code</TableHead>
                <TableHead>Discount</TableHead>
                <TableHead>Minimum</TableHead>
                <TableHead>Valid</TableHead>
                <TableHead>Uses</TableHead>
                <TableHead>State</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {codes?.map((code) => {
                const { label, variant } = state(code);
                return (
                  <TableRow key={code.id}>
                    <TableCell className="font-medium">{code.code}</TableCell>
                    <TableCell>{describeDiscount(code)}</TableCell>
                    <TableCell>{code.minimumSubtotal ? formatAsDollars(code.minimumSubtotal) : '—'}</TableCell>
                    <TableCell className="text-muted-foreground">
                      {code.startsAt ? formatDate(code.startsAt) : 'Now'} – {code.expiresAt ? formatDate(code.expiresAt) : 'no end'}
                    </TableCell>
                    <TableCell className="tabular-nums">
                      {code.timesUsed}
                      {code.usageLimit ? ` / ${code.usageLimit}` : ''}
                    </TableCell>
                    <TableCell>
                      <Badge variant={variant}>{label}</Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      <Button type="button" variant="ghost" size="icon" aria-label={`Edit ${code.code}`} onClick={() => setEditing({ id: code.id, draft: toDraft(code) })}>
                        <Pencil />
                      </Button>
                      <Button type="button" variant="ghost" size="icon" aria-label={`Delete ${code.code}`} onClick={() => setToDelete(code)}>
                        <Trash2 />
                      </Button>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        )}
      </Card>

      <Sheet open={editing !== null} onOpenChange={(open) => !open && setEditing(null)}>
        <SheetContent className="overflow-y-auto sm:max-w-md">
          <SheetHeader>
            <SheetTitle>{editing?.id === undefined ? 'New discount code' : `Edit ${draft?.code}`}</SheetTitle>
            <SheetDescription>Customers type the code in the cart. It never adds up with the first-order discount: the larger one is taken.</SheetDescription>
          </SheetHeader>
          {draft && (
            <form className="mt-6 grid gap-5" onSubmit={submit}>
              <TextField label="Code" value={draft.code} onChange={(value) => set('code', value.toUpperCase())} required maxLength={32} hint="Letters, digits and dashes." />
              <Field label="Kind">
                <div role="radiogroup" aria-label="Kind" className="flex gap-2">
                  {(['Percent', 'Amount'] as const).map((kind) => (
                    <Button key={kind} type="button" role="radio" aria-checked={draft.kind === kind} variant={draft.kind === kind ? 'default' : 'outline'} onClick={() => set('kind', kind)}>
                      {kind === 'Percent' ? 'Percentage' : 'Fixed amount'}
                    </Button>
                  ))}
                </div>
              </Field>
              <TextField label={draft.kind === 'Percent' ? 'Percent off' : 'Dollars off'} type="number" value={draft.value} onChange={(value) => set('value', value)} required />
              <TextField label="Minimum order" type="number" value={draft.minimumSubtotal} onChange={(value) => set('minimumSubtotal', value)} hint="Empty for any order." />
              <div className="grid gap-5 sm:grid-cols-2">
                <TextField label="Starts" type="datetime-local" value={draft.startsAt} onChange={(value) => set('startsAt', value)} />
                <TextField label="Ends" type="datetime-local" value={draft.expiresAt} onChange={(value) => set('expiresAt', value)} />
              </div>
              <TextField label="Usage limit" type="number" value={draft.usageLimit} onChange={(value) => set('usageLimit', value)} hint="Orders in total; empty for no limit." />
              <label className="flex items-center gap-3 text-sm">
                <input type="checkbox" checked={draft.isActive} onChange={(event) => set('isActive', event.target.checked)} className="h-4 w-4 accent-foreground" />
                Active
              </label>
              <div className="flex justify-end gap-2">
                <Button type="button" variant="outline" onClick={() => setEditing(null)}>
                  Cancel
                </Button>
                <Button type="submit" disabled={saving}>
                  Save
                </Button>
              </div>
            </form>
          )}
        </SheetContent>
      </Sheet>

      <ConfirmDialog
        open={toDelete !== null}
        title={`Delete ${toDelete?.code}?`}
        description="Only a code no order used can be deleted; a used one can be switched off instead."
        confirmLabel="Delete"
        busy={deleting}
        onConfirm={confirmDelete}
        onCancel={() => setToDelete(null)}
      />
    </div>
  );
};

export default DiscountCodes;
