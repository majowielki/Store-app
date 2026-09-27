import React from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { SlidersHorizontal } from 'lucide-react';
import type { ProductQuery, ProductsMeta } from '@/api/types';
import { useIsMobile } from '@/hooks/use-mobile';
import { Button } from './ui/button';
import { Sheet, SheetContent, SheetDescription, SheetTitle } from './ui/sheet';
import FormCheckbox from './FormCheckbox';
import FormInput from './FormInput';
import FormRange from './FormRange';
import FormSelect from './FormSelect';

interface FiltersProps {
  meta: ProductsMeta;
  /** The query the listing currently shows, taken from the URL. */
  query: ProductQuery;
}

const capitalize = (s: string) => s.charAt(0).toUpperCase() + s.slice(1);
const toSlug = (s: string) => s.toLowerCase().replace(/\s+/g, '');

/** The filter fields; submitting puts them in the URL, which is what the listing reads. */
const FiltersForm = ({ meta, query, onSubmitted }: FiltersProps & { onSubmitted?: () => void }) => {
  const [searchParams, setSearchParams] = useSearchParams();
  const { search, company, category, color, order, price, group, sale } = query;

  const groupList = React.useMemo(() => (meta.groups.length > 0 ? meta.groups : ['all']), [meta.groups]);
  const initialGroup = group && groupList.includes(group) ? group : 'all';
  // Controlled, so the category options follow the group at once; the URL changes on Search
  const [groupValue, setGroupValue] = React.useState(initialGroup);
  React.useEffect(() => {
    setGroupValue(initialGroup);
  }, [initialGroup]);

  // The categories of the selected group; every category when no group is selected
  const categoryOptions = React.useMemo(() => {
    const selected = meta.groupCategoryMap.find((entry) => entry.key.toLowerCase() === groupValue.toLowerCase());
    if (selected && selected.categories.length > 0) {
      return selected.categories.map((cat) => ({ value: cat.key, label: capitalize(cat.name) }));
    }
    if (groupValue === 'all') {
      return meta.categories.map((cat) => ({ value: toSlug(cat), label: capitalize(cat) }));
    }
    return [];
  }, [groupValue, meta.groupCategoryMap, meta.categories]);

  const groupOptions = groupList.map((g) => ({ value: g, label: capitalize(g) }));
  const companyOptions = meta.companies.filter((c) => c.toLowerCase() !== 'all').map((c) => ({ value: c, label: capitalize(c) }));
  const colorOptions = meta.colors.map((c) => ({ value: c, label: capitalize(c) }));
  const categoryDefault = groupValue === initialGroup ? category : undefined;

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const next = new URLSearchParams(new FormData(e.currentTarget) as unknown as Record<string, string>);
    // "all" and empty values mean no filter; a new search starts on the first page
    for (const key of ['search', 'group', 'category', 'company', 'color', 'order', 'price', 'sale']) {
      const value = next.get(key);
      if (!value || value === 'all') next.delete(key);
    }
    const layout = searchParams.get('layout');
    if (layout) next.set('layout', layout);
    setSearchParams(next);
    onSubmitted?.();
  };

  return (
    // Remounted on every URL change, so the fields show the query the page is on (reset included)
    <form key={searchParams.toString()} aria-label="Product filters" className="grid gap-5" onSubmit={handleSubmit}>
      <FormInput type="search" label="search product" name="search" defaultValue={search} placeholder="Oak, sofa, lamp…" />
      <FormSelect label="select group" name="group" options={groupOptions} includeAll value={groupValue} onValueChange={setGroupValue} />
      <FormSelect
        key={`category-${groupValue}`}
        label="select category"
        name="category"
        options={categoryOptions}
        defaultValue={categoryDefault}
        includeAll
      />
      <FormSelect label="select company" name="company" options={companyOptions} defaultValue={company} includeAll />
      <FormSelect label="select color" name="color" options={colorOptions} defaultValue={color} includeAll />
      <FormSelect label="order by" name="order" options={['a-z', 'z-a', 'high', 'low']} defaultValue={order} includeAll />
      <FormRange label="price" name="price" defaultValue={price} />
      <FormCheckbox name="sale" label="sale only" defaultValue={sale} />
      <div className="flex items-center gap-2 pt-1">
        <Button type="submit" className="flex-1">
          Search
        </Button>
        <Button type="button" asChild variant="ghost">
          <Link to="/products" onClick={onSubmitted}>
            Reset
          </Link>
        </Button>
      </div>
    </form>
  );
};

/**
 * The catalogue's filters: a sticky column beside the products on wider screens, a sheet
 * rising from the bottom on phones.
 */
const Filters = (props: FiltersProps) => {
  const isMobile = useIsMobile();
  const [open, setOpen] = React.useState(false);

  if (isMobile) {
    return (
      <>
        <Button type="button" variant="outline" className="w-full" onClick={() => setOpen(true)}>
          <SlidersHorizontal />
          Filters &amp; sorting
        </Button>
        <Sheet open={open} onOpenChange={setOpen}>
          <SheetContent side="bottom" className="max-h-[88vh] overflow-y-auto rounded-t-4xl px-5 pb-8 pt-6">
            <SheetTitle className="display mb-5 text-3xl font-normal">Filters</SheetTitle>
            <SheetDescription className="sr-only">Narrow the products down and choose their order</SheetDescription>
            <FiltersForm {...props} onSubmitted={() => setOpen(false)} />
          </SheetContent>
        </Sheet>
      </>
    );
  }

  return (
    <aside className="no-scrollbar -mx-1 px-1 md:sticky md:top-24 md:max-h-[calc(100vh-7rem)] md:self-start md:overflow-y-auto md:pb-6">
      <p className="display mb-5 text-2xl">Filters</p>
      <FiltersForm {...props} />
    </aside>
  );
};

export default Filters;
