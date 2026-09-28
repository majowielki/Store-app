import { Link } from 'react-router-dom';
import { ChevronRight } from 'lucide-react';
import type { ProductQuery, ProductsMeta } from '@/api/types';
import { cn } from '@/lib/utils';
import { PageMeta } from '@/seo';
import { categories, categoryHref } from '@/utils/categories';

interface CatalogHeaderProps {
  meta: ProductsMeta;
  query: ProductQuery;
}

const isSale = (sale?: string) => sale === 'on' || sale === 'true';

/** What the listing is showing, in words: the title, a line under it and the trail back to the shop. */
const describe = ({ meta, query }: CatalogHeaderProps) => {
  const group = meta.groupCategoryMap.find((g) => g.key === query.group);
  const category = group?.categories.find((c) => c.key === query.category);
  if (query.search && meta.searchCorrection) {
    return {
      title: `“${meta.searchCorrection}”`,
      eyebrow: 'Search results',
      lead: `Nothing matched “${query.search}”, so these are the results for “${meta.searchCorrection}”.`,
      group,
    };
  }
  if (query.search) {
    return { title: `“${query.search}”`, eyebrow: 'Search results', lead: 'Everything in the shop matching your search.', group };
  }
  if (isSale(query.sale) && !group) {
    return { title: 'Sale', eyebrow: 'While stocks last', lead: 'Pieces reduced for a while — the price you see is the price you pay.', group };
  }
  if (category) {
    return { title: category.name, eyebrow: group?.name ?? 'Shop', lead: `${category.name} from our ${group?.name.toLowerCase()} collection.`, group };
  }
  if (group) {
    // Lower case reads better in a sentence, except for abbreviations such as TV
    const names = group.categories.map((c) =>
      c.name
        .split(' ')
        .map((word) => (word.length > 1 && word === word.toUpperCase() ? word : word.toLowerCase()))
        .join(' '),
    );
    const lead = names.length > 1 ? `${names.slice(0, -1).join(', ')} and ${names[names.length - 1]}.` : `${names[0] ?? 'Everything'} and more.`;
    return { title: group.name, eyebrow: 'Shop by room', lead: lead.charAt(0).toUpperCase() + lead.slice(1), group };
  }
  return { title: 'All products', eyebrow: 'The shop', lead: 'Every piece we make, from sofas to garden sets.', group };
};

/** The top of the catalogue: breadcrumbs, a large title and the rooms as quick links. */
const CatalogHeader = ({ meta, query }: CatalogHeaderProps) => {
  const { title, eyebrow, lead, group } = describe({ meta, query });
  const sale = isSale(query.sale);
  const noGroup = !query.group || query.group === 'all';

  return (
    <header className="border-b pb-8">
      {/* A search's results are no page of their own for search engines */}
      <PageMeta title={query.search ? `${eyebrow}: ${title}` : title} description={lead} noindex={Boolean(query.search)} />
      <nav aria-label="Breadcrumb" className="flex items-center gap-1.5 text-xs text-muted-foreground">
        <Link to="/" className="transition-colors hover:text-foreground">
          Home
        </Link>
        <ChevronRight className="h-3 w-3" />
        <Link to="/products" className="transition-colors hover:text-foreground">
          Shop
        </Link>
        {group && (
          <>
            <ChevronRight className="h-3 w-3" />
            <Link to={`/products?group=${encodeURIComponent(group.key)}`} className="transition-colors hover:text-foreground">
              {group.name}
            </Link>
          </>
        )}
      </nav>
      <div key={title} className="mt-8 animate-fade-up">
        <p className="eyebrow">{eyebrow}</p>
        <h1 className="display mt-3 text-5xl leading-[0.95] md:text-7xl">{title}</h1>
        <p className="mt-4 max-w-lg text-muted-foreground">{lead}</p>
      </div>
      <ul className="no-scrollbar -mx-4 mt-8 flex gap-2 overflow-x-auto px-4 sm:mx-0 sm:flex-wrap sm:px-0">
        <li>
          <Link
            to="/products"
            className={cn(
              'inline-flex h-9 items-center whitespace-nowrap rounded-full border px-4 text-sm transition-colors',
              noGroup && !sale && !query.search ? 'border-foreground bg-foreground text-background' : 'hover:border-foreground',
            )}
          >
            Everything
          </Link>
        </li>
        {categories.map((c) => {
          const active = c.group === 'sale' ? sale && noGroup : query.group === c.group;
          return (
            <li key={c.slug}>
              <Link
                to={categoryHref(c)}
                className={cn(
                  'inline-flex h-9 items-center whitespace-nowrap rounded-full border px-4 text-sm transition-colors',
                  active
                    ? c.group === 'sale'
                      ? 'border-brand bg-brand text-brand-foreground'
                      : 'border-foreground bg-foreground text-background'
                    : c.group === 'sale'
                      ? 'text-brand hover:border-brand'
                      : 'hover:border-foreground',
                )}
              >
                {c.label}
              </Link>
            </li>
          );
        })}
      </ul>
    </header>
  );
};

export default CatalogHeader;
