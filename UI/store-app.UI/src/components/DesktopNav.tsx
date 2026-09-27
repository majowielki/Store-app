import { useEffect, useRef, useState } from 'react';
import { Link, NavLink, useLocation, useSearchParams } from 'react-router-dom';
import { ArrowRight } from 'lucide-react';
import { useGetProductsMetaQuery, useGetProductsQuery } from '@/api/catalog';
import { useGetArticlesQuery, useGetLookbooksQuery } from '@/api/content';
import { inspirationPages } from '@/content/pages';
import { cn } from '@/lib/utils';
import { categories, categoryHref } from '@/utils/categories';
import { ProductPrice } from './ProductCard';
import { useOpenProduct } from '@/hooks/use-open-product';

const itemClass = (active: boolean, sale = false) =>
  cn(
    'relative inline-flex h-10 items-center px-3 text-sm font-medium transition-colors',
    // The underline grows from the left on hover and stays under the section the page is in
    'after:absolute after:inset-x-3 after:bottom-1.5 after:h-px after:origin-left after:bg-current after:transition-transform after:duration-500 after:ease-smooth',
    active ? 'after:scale-x-100' : 'after:scale-x-0 hover:after:scale-x-100',
    sale ? 'text-brand' : active ? 'text-foreground' : 'text-foreground/70 hover:text-foreground',
  );

/** The categories of one group and two of its products, under the header while the group is hovered. */
const MegaPanel = ({ group, onNavigate }: { group: string; onNavigate: () => void }) => {
  const { data: meta } = useGetProductsMetaQuery();
  const { data: picks } = useGetProductsQuery({ group, pageSize: 2 });
  const openProduct = useOpenProduct();
  const entry = meta?.groupCategoryMap.find((g) => g.key === group);
  const category = categories.find((c) => c.group === group);
  const name = entry?.name ?? category?.label ?? group;

  return (
    <div className="align-element grid grid-cols-12 gap-10 py-10">
      <div className="col-span-3">
        <p className="eyebrow">Shop by room</p>
        <p className="display mt-3 text-5xl">{name}</p>
        <Link
          to={category ? categoryHref(category) : `/products?group=${encodeURIComponent(group)}`}
          onClick={onNavigate}
          className="group/all mt-6 inline-flex items-center gap-2 text-sm font-medium"
        >
          <span className="link-underline">Shop all {name.toLowerCase()}</span>
          <ArrowRight className="h-4 w-4 transition-transform duration-300 group-hover/all:translate-x-1" />
        </Link>
      </div>
      <ul className="col-span-4 grid grid-cols-2 content-start gap-x-6 gap-y-3 border-l pl-10">
        {entry?.categories.map((c, index) => (
          <li key={c.key} className="animate-fade-up" style={{ animationDelay: `${index * 30}ms` }}>
            <Link
              to={`/products?group=${encodeURIComponent(group)}&category=${encodeURIComponent(c.key)}`}
              onClick={onNavigate}
              className="text-sm text-muted-foreground transition-colors hover:text-foreground"
            >
              {c.name}
            </Link>
          </li>
        ))}
      </ul>
      <div className="col-span-5 grid grid-cols-2 gap-5">
        {picks?.items.map((product) => (
          <Link
            key={product.id}
            to={`/products/${product.id}`}
            onClick={() => {
              openProduct(product);
              onNavigate();
            }}
            className="group/pick block"
          >
            <div className="aspect-4/3 overflow-hidden rounded-xl bg-muted">
              <img
                src={product.image}
                alt={product.title}
                className="h-full w-full object-cover transition-transform duration-700 ease-smooth group-hover/pick:scale-105"
              />
            </div>
            <div className="mt-3 flex items-start justify-between gap-3 text-sm">
              <span className="font-medium">{product.title}</span>
              <ProductPrice product={product} stacked />
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
};

const INSPIRATION = 'inspiration';

/** The editorial sections and, next to them, the first look and the newest journal article. */
const InspirationPanel = ({ onNavigate }: { onNavigate: () => void }) => {
  const { data: looks } = useGetLookbooksQuery();
  const { data: articles } = useGetArticlesQuery();
  const picks = [
    looks?.[0] && { to: `/looks/${looks[0].slug}`, image: looks[0].image, eyebrow: 'Shop the look', title: looks[0].title },
    articles?.[0] && { to: `/journal/${articles[0].slug}`, image: articles[0].coverImage, eyebrow: 'Journal', title: articles[0].title },
  ].filter((pick): pick is { to: string; image: string; eyebrow: string; title: string } => Boolean(pick));

  return (
    <div className="align-element grid grid-cols-12 gap-10 py-10">
      <div className="col-span-3">
        <p className="eyebrow">Ideas and stories</p>
        <p className="display mt-3 text-5xl">Inspiration</p>
      </div>
      <ul className="col-span-3 grid content-start gap-3 border-l pl-10">
        {inspirationPages.map((page, index) => (
          <li key={page.to} className="animate-fade-up" style={{ animationDelay: `${index * 30}ms` }}>
            <Link to={page.to} onClick={onNavigate} className="text-sm text-muted-foreground transition-colors hover:text-foreground">
              {page.label}
            </Link>
          </li>
        ))}
      </ul>
      <div className="col-span-6 grid grid-cols-2 gap-5">
        {picks.map((pick) => (
          <Link key={pick.to} to={pick.to} onClick={onNavigate} className="group/pick block">
            <div className="aspect-16/10 overflow-hidden rounded-xl bg-muted">
              <img src={pick.image} alt="" className="h-full w-full object-cover transition-transform duration-700 ease-smooth group-hover/pick:scale-105" />
            </div>
            <p className="eyebrow mt-3">{pick.eyebrow}</p>
            <p className="mt-1 text-sm font-medium">{pick.title}</p>
          </Link>
        ))}
      </div>
    </div>
  );
};

/** The shop's sections in the header (wide screens), each group opening its panel on hover. */
const DesktopNav = ({ className }: { className?: string }) => {
  const [openGroup, setOpenGroup] = useState<string | null>(null);
  const closeTimer = useRef<number | undefined>(undefined);
  const location = useLocation();
  const [searchParams] = useSearchParams();

  const onProducts = location.pathname === '/products';
  const currentGroup = onProducts ? searchParams.get('group') : null;
  const saleActive = onProducts && (searchParams.get('sale') === 'on' || searchParams.get('sale') === 'true');
  const allActive = onProducts && !saleActive && (!currentGroup || currentGroup === 'all');

  const cancelClose = () => window.clearTimeout(closeTimer.current);
  const open = (group: string | null) => {
    cancelClose();
    setOpenGroup(group);
  };
  const scheduleClose = () => {
    cancelClose();
    closeTimer.current = window.setTimeout(() => setOpenGroup(null), 160);
  };

  // A new page closes the panel; so does Escape
  useEffect(() => setOpenGroup(null), [location.key]);
  useEffect(() => {
    if (!openGroup) return;
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpenGroup(null);
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [openGroup]);
  useEffect(() => () => window.clearTimeout(closeTimer.current), []);

  return (
    <nav aria-label="Shop sections" className={className} onMouseLeave={scheduleClose} onMouseEnter={cancelClose}>
      <ul className="flex items-center">
        <li onMouseEnter={() => open(null)}>
          <NavLink to="/products" className={() => itemClass(allActive)}>
            Shop all
          </NavLink>
        </li>
        {categories.map((c) => {
          const sale = c.group === 'sale';
          return (
            <li key={c.slug} onMouseEnter={() => open(sale ? null : c.group)}>
              <NavLink
                to={categoryHref(c)}
                className={() => itemClass(sale ? saleActive : onProducts && currentGroup === c.group, sale)}
              >
                {c.label}
              </NavLink>
            </li>
          );
        })}
      </ul>
      {openGroup && (
        <div className="absolute inset-x-0 top-full border-b bg-background/95 shadow-[0_32px_64px_-32px_rgba(0,0,0,0.25)] backdrop-blur-xl animate-in fade-in-0 slide-in-from-top-1 duration-300">
          {openGroup === INSPIRATION ? (
            <InspirationPanel onNavigate={() => setOpenGroup(null)} />
          ) : (
            <MegaPanel key={openGroup} group={openGroup} onNavigate={() => setOpenGroup(null)} />
          )}
        </div>
      )}
    </nav>
  );
};

export default DesktopNav;
