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
      return groupCats.map((cat) => {
        if (typeof cat === 'string') return cat;
        if (cat && typeof cat === 'object') {
          return (cat as { name?: string; label?: string; slug?: string }).name || (cat as { label?: string; slug?: string }).label || (cat as { slug?: string }).slug || '';
        }
        return '';
      }).filter((s) => typeof s === 'string' && s.length > 0);
    }
    // fallback: only show all categories if group is 'all', otherwise show empty
    return groupKey === 'all' ? meta.categories : [];
  }, [groupValue, meta.groupCategoryMap, meta.categories]);
  // Capitalize display labels for groups, categories, companies, colors
  const capitalize = (s: string) => s.charAt(0).toUpperCase() + s.slice(1);
  // Include 'all' in groupOptions so the Select Group dropdown can display 'All' as a valid selection
  const groupOptions = (Array.isArray(meta.groups) && meta.groups.length > 0 ? meta.groups : ['all'])
    .map(capitalize);
  const categoriesFromMapCapitalized = categoriesFromMap.map(capitalize);
  const companiesCapitalized = (meta.companies || [])
    .filter(c => c.toLowerCase() !== 'all')
    .map(capitalize);
  const colorsCapitalized = (meta.colors || []).map(capitalize);
  const categoryDefault = groupValue === initialGroup ? (category ? capitalize(category) : undefined) : undefined;

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
              defaultValue={group ? capitalize(group) : undefined}
              includeAll
              value={groupValue ? capitalize(groupValue) : undefined}
              onValueChange={(val) => {
                // Map back to original value (lowercase)
                const original = (meta.groups?.find((g: string) => capitalize(g) === val)) || val?.toLowerCase() || 'all';
                setGroupValue(original);
                const sp = new URLSearchParams(location.search);
                if (!original || original === 'all') {
                  sp.delete('group');
                } else {
                  sp.set('group', original);
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
            options={categoriesFromMapCapitalized}
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