import React from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import type { ProductQuery, ProductsMeta } from '@/api/types';
import { useIsMobile } from '@/hooks/use-mobile';
import { Button } from './ui/button';
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

const Filters = ({ meta, query }: FiltersProps) => {
  const isMobile = useIsMobile();
  const [showFilters, setShowFilters] = React.useState(() => !isMobile);
  React.useEffect(() => {
    setShowFilters(!isMobile);
  }, [isMobile]);

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
  };

  return (
    <div className="mb-4">
      {isMobile && (
        <Button type="button" className="mb-2 w-full" variant="outline" onClick={() => setShowFilters((v) => !v)}>
          {showFilters ? 'Hide Filters' : 'Show Filters'}
        </Button>
      )}
      {showFilters && (
        // Remounted on every URL change, so the fields show the query the page is on (reset included)
        <form
          key={searchParams.toString()}
          aria-label="Product filters"
          className="border rounded-md px-8 py-4 grid gap-x-4 gap-y-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 items-center"
          onSubmit={handleSubmit}
        >
          <FormInput type="search" label="search product" name="search" defaultValue={search} />
          <FormSelect
            label="select group"
            name="group"
            options={groupOptions}
            includeAll
            value={groupValue}
            onValueChange={setGroupValue}
          />
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
          <div className="sm:col-span-2 md:col-span-3 lg:col-span-4 flex justify-end items-end gap-2">
            <Button type="submit" size="sm">
              Search
            </Button>
            <Button type="button" asChild size="sm" variant="outline">
              <Link to="/products">reset</Link>
            </Button>
          </div>
        </form>
      )}
    </div>
  );
};

export default Filters;
