import { Link, useLocation } from 'react-router-dom';
import { X } from 'lucide-react';
import type { ProductsMeta } from '@/api/types';
import { formatAsDollars } from '@/utils';

const capitalize = (s: string) => s.charAt(0).toUpperCase() + s.slice(1);

const orderLabels: Record<string, string> = { 'a-z': 'A to Z', 'z-a': 'Z to A', high: 'Price, high to low', low: 'Price, low to high' };

/**
 * The filters the listing is narrowed by, as chips: each one a link to the same listing
 * without it. Nothing shows while nothing is filtered.
 */
const ActiveFilters = ({ meta }: { meta: ProductsMeta }) => {
  const { search, pathname } = useLocation();
  const params = new URLSearchParams(search);

  const without = (...keys: string[]) => {
    const next = new URLSearchParams(params);
    for (const key of [...keys, 'page']) next.delete(key);
    const query = next.toString();
    return query ? `${pathname}?${query}` : pathname;
  };

  const group = meta.groupCategoryMap.find((g) => g.key === params.get('group'));
  const chips: { key: string; name: string; label: React.ReactNode; to: string }[] = [];
  const add = (key: string, name: string, label: React.ReactNode = name, ...alsoRemove: string[]) =>
    chips.push({ key, name, label, to: without(key, ...alsoRemove) });

  const text = params.get('search');
  if (text) add('search', `“${text}”`);
  if (group) add('group', group.name, group.name, 'category');
  const category = params.get('category');
  if (category) add('category', group?.categories.find((c) => c.key === category)?.name ?? capitalize(category));
  const company = params.get('company');
  if (company) add('company', capitalize(company));
  const color = params.get('color');
  if (color) {
    add(
      'color',
      capitalize(color),
      <>
        <span className="h-3 w-3 rounded-full border border-foreground/20" style={{ backgroundColor: color }} />
        {capitalize(color)}
      </>,
    );
  }
  const price = params.get('price');
  if (price && price !== '0,2000') {
    const [lo, hi] = price.replace('-', ',').split(',').map(Number);
    if (Number.isFinite(lo) && Number.isFinite(hi)) add('price', `${formatAsDollars(lo)} – ${formatAsDollars(hi)}`);
  }
  const sale = params.get('sale');
  if (sale === 'on' || sale === 'true') add('sale', 'On sale');
  const order = params.get('order');
  if (order && orderLabels[order]) add('order', orderLabels[order]);

  if (chips.length === 0) return null;

  return (
    <div className="mb-6 flex flex-wrap items-center gap-2">
      {chips.map((chip) => (
        <Link
          key={chip.key}
          to={chip.to}
          aria-label={`Remove filter: ${chip.name}`}
          className="group inline-flex h-8 animate-in fade-in-0 zoom-in-95 items-center gap-2 rounded-full bg-secondary pl-3 pr-2 text-xs font-medium transition-colors hover:bg-foreground hover:text-background"
        >
          {chip.label}
          <X className="h-3.5 w-3.5 opacity-60 group-hover:opacity-100" />
        </Link>
      ))}
      {chips.length > 1 && (
        <Link to={pathname} className="link-underline ml-1 text-xs text-muted-foreground hover:text-foreground">
          Clear all
        </Link>
      )}
    </div>
  );
};

export default ActiveFilters;
