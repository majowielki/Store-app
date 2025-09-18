import React from 'react';
import { useIsMobile } from '@/hooks/use-mobile';
import { Form, useLoaderData, Link, useLocation } from "react-router-dom";
import { Button } from "./ui/button";
import { ProductsResponseWithParams } from "@/utils";
import FormInput from "./FormInput";
import FormSelect from "./FormSelect";
import FormRange from "./FormRange";
import FormCheckbox from "./FormCheckbox";
// shipping checkbox removed

const Filters = () => {
  // Capitalize display labels for groups, companies, colors
  const capitalize = (s: string) => s.charAt(0).toUpperCase() + s.slice(1);
  const isMobile = useIsMobile();
  const [showFilters, setShowFilters] = React.useState(() => !isMobile);
  // Update showFilters if screen size changes
  React.useEffect(() => {
    setShowFilters(!isMobile);
  }, [isMobile]);
  const { meta, params } = useLoaderData() as ProductsResponseWithParams;
  const { search, company, category, color, order, price, group, sale } = params as Record<string, string>;

  const location = useLocation();
  const queryParams = new URLSearchParams(location.search);
  const layout = queryParams.get("layout") || "grid";

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
  const formData = new FormData(e.currentTarget);
  const searchParams = new URLSearchParams(formData as unknown as URLSearchParams);
    // Remove no-op filters (value === 'all' or empty)
    ['group','category','company','color','order'].forEach((key) => {
      const v = searchParams.get(key);
      if (!v || v === 'all') searchParams.delete(key);
    });
    searchParams.set("layout", layout); // Preserve layout
    window.location.search = searchParams.toString();
  };

  // Controlled group value so category options update immediately on change
  const groupList = React.useMemo(() => (
    Array.isArray(meta.groups) && meta.groups.length > 0 ? meta.groups : ['all']
  ), [meta.groups]);
  const isValidGroup = group && groupList.includes(group);
  const initialGroup = isValidGroup ? group : 'all';
  const [groupValue, setGroupValue] = React.useState<string>(initialGroup);
  // Sync groupValue with group param in URL/location
  React.useEffect(() => {
    const valid = group && groupList.includes(group);
    setGroupValue(valid ? group : 'all');
  }, [group, location.key, groupList]);
  // Map groupCategoryMap (array) to lookup and extract categories for selected group (case-insensitive)
  const categoriesFromMap = React.useMemo(() => {
  const toSlug = (str: string) => str.toLowerCase().replace(/\s+/g, '');
    const mapArr = Array.isArray(meta.groupCategoryMap) ? meta.groupCategoryMap : [];
    // Build lookup: lowercased key -> { name, categories }
    const lookup: Record<string, { name: string; categories: unknown[] }> = {};
    for (const entry of mapArr) {
      if (entry && typeof entry === 'object' && 'key' in entry && 'categories' in entry) {
        const key = ((entry as { key: string }).key || '').toLowerCase();
        const name = (entry as { name?: string }).name || key;
        const categories = Array.isArray((entry as { categories: unknown[] }).categories) ? (entry as { categories: unknown[] }).categories : [];
        lookup[key] = { name, categories };
      }
    }
    // Only show categories for the selected group (case-insensitive)
    const groupKey = (groupValue || '').toLowerCase();
    const groupCats = lookup[groupKey]?.categories;
    if (Array.isArray(groupCats) && groupCats.length > 0) {
      // Return array of { value, label } with value as slug
      return groupCats.map((cat) => {
        let value = '';
        let label = '';
        if (typeof cat === 'string') {
          value = toSlug(cat);
          label = capitalize(cat);
        } else if (cat && typeof cat === 'object') {
          value = (cat as { slug?: string; name?: string; label?: string }).slug
            || (cat as { name?: string }).name && toSlug((cat as { name?: string }).name!)
            || (cat as { label?: string }).label && toSlug((cat as { label?: string }).label!)
            || '';
          label = capitalize(
            (cat as { name?: string }).name
            || (cat as { label?: string }).label
            || (cat as { slug?: string }).slug
            || ''
          );
        }
        return { value, label };
      }).filter((c) => typeof c.value === 'string' && c.value.length > 0);
    }
    // fallback: only show all categories if group is 'all', otherwise show empty
    if (groupKey === 'all') {
      return (meta.categories || []).map((cat: string) => ({
        value: toSlug(cat),
        label: capitalize(cat),
      }));
    }
    return [];
  }, [groupValue, meta.groupCategoryMap, meta.categories]);
  // ...existing code...
  // Include 'all' in groupOptions so the Select Group dropdown can display 'All' as a valid selection
  // Use original group values for options, but display capitalized labels
  const groupOptions = (Array.isArray(meta.groups) && meta.groups.length > 0 ? meta.groups : ['all'])
    .map((g) => ({ value: g, label: capitalize(g) }));
  const companiesCapitalized = (meta.companies || [])
    .filter(c => c.toLowerCase() !== 'all')
    .map(c => ({ value: c, label: capitalize(c) }));
  const colorsCapitalized = (meta.colors || [])
    .map(c => ({ value: c, label: capitalize(c) }));
  // Use the raw category value for defaultValue
  const categoryDefault = groupValue === initialGroup ? (category ? category : undefined) : undefined;

  return (
    <div className="mb-4">
      {isMobile && (
        <Button
          type="button"
          className="mb-2 w-full"
          variant="outline"
          onClick={() => setShowFilters((v) => !v)}
        >
          {showFilters ? 'Hide Filters' : 'Show Filters'}
        </Button>
      )}
      {showFilters && (
        <Form
          className="border rounded-md px-8 py-4 grid gap-x-4 gap-y-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 items-center"
          onSubmit={handleSubmit}
        >
          {/* SEARCH */}
          <FormInput
            type="search"
            label="search product"
            name="search"
            defaultValue={search}
          />
          {/* GROUPS */}
          {groupOptions && groupOptions.length > 0 && (
            <FormSelect
              label="select group"
              name="group"
              options={groupOptions}
              defaultValue={groupValue}
              includeAll
              value={groupValue}
              onValueChange={(val) => {
                setGroupValue(val);
                const sp = new URLSearchParams(location.search);
                if (!val || val === 'all') {
                  sp.delete('group');
                } else {
                  sp.set('group', val);
                }
                // Reset category when group changes to avoid invalid selections
                sp.delete('category');
                // Preserve layout
                if (layout) sp.set('layout', layout);
                window.history.replaceState(null, '', `${location.pathname}?${sp.toString()}`);
              }}
            />
          )}
          {/* CATEGORIES */}
          <FormSelect
            key={`category-${groupValue}`}
            label="select category"
            name="category"
            options={categoriesFromMap}
            defaultValue={categoryDefault}
            includeAll
          />
          {/* COMPANIES */}
          <FormSelect
            label="select company"
            name="company"
            options={companiesCapitalized}
            defaultValue={company ? capitalize(company) : undefined}
            includeAll
          />
          {/* COLOR */}
          <FormSelect
            label="select color"
            name="color"
            options={colorsCapitalized}
            defaultValue={color ? capitalize(color) : undefined}
            includeAll
          />
          {/* ORDER */}
          <FormSelect
            label="order by"
            name="order"
            options={["a-z", "z-a", "high", "low"]}
            defaultValue={order}
            includeAll
          />
          {/* PRICE */}
          <FormRange label="price" name="price" defaultValue={price} />
          {/* SALE ONLY */}
          <FormCheckbox name="sale" label="sale only" defaultValue={sale} />
          <div className="sm:col-span-2 md:col-span-3 lg:col-span-4 flex justify-end items-end gap-2">
            <Button type="submit" size="sm">
              Search
            </Button>
            <Button type="button" asChild size="sm" variant="outline">
              <Link to="/products">reset</Link>
            </Button>
          </div>
        </Form>
      )}
    </div>
  );
};

export default Filters;