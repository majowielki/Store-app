import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ArrowRight, Loader2, Search } from 'lucide-react';
import { useGetProductsQuery } from '@/api/catalog';
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/components/ui/sheet';
import { useOpenProduct } from '@/hooks/use-open-product';
import { categories, categoryHref } from '@/utils/categories';
import ProductPrice from './ProductPrice';
import RecentlyViewed from './RecentlyViewed';

const useDebounced = (value: string, delay: number) => {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delay);
    return () => window.clearTimeout(timer);
  }, [value, delay]);
  return debounced;
};

interface SearchDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/**
 * The shop's search, dropping from the top of the screen: matching products appear while
 * typing, Enter opens the full listing for the words typed.
 */
const SearchDialog = ({ open, onOpenChange }: SearchDialogProps) => {
  const navigate = useNavigate();
  const openProduct = useOpenProduct();
  const [text, setText] = useState('');
  const query = useDebounced(text.trim(), 250);
  const searching = query.length >= 2;
  const { data, isFetching } = useGetProductsQuery({ search: query, pageSize: 6 }, { skip: !open || !searching });
  const results = searching ? (data?.items ?? []) : [];

  useEffect(() => {
    if (!open) setText('');
  }, [open]);

  const close = () => onOpenChange(false);
  const showAll = (e?: React.FormEvent) => {
    e?.preventDefault();
    const words = text.trim();
    close();
    navigate(words ? `/products?search=${encodeURIComponent(words)}` : '/products');
  };

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="top" className="max-h-[90vh] overflow-y-auto p-0">
        <SheetTitle className="sr-only">Search the shop</SheetTitle>
        <SheetDescription className="sr-only">Matching products show up as you type; Enter lists them all.</SheetDescription>
        <div className="align-element py-10 md:py-14">
          <form role="search" onSubmit={showAll} className="flex items-center gap-4 border-b-2 border-foreground pb-3">
            <Search className="h-6 w-6 shrink-0 text-muted-foreground md:h-8 md:w-8" aria-hidden />
            <input
              autoFocus
              type="search"
              value={text}
              onChange={(e) => setText(e.target.value)}
              placeholder="What are you looking for?"
              aria-label="Search products"
              className="display w-full min-w-0 bg-transparent text-3xl outline-none placeholder:text-muted-foreground/50 md:text-5xl [&::-webkit-search-cancel-button]:hidden"
            />
            {isFetching && <Loader2 className="h-5 w-5 shrink-0 animate-spin text-muted-foreground" aria-label="Searching" />}
          </form>

          {!searching ? (
            <div className="mt-8">
              <p className="eyebrow">Browse by room</p>
              <ul className="mt-4 flex flex-wrap gap-2">
                {categories.map((c, index) => (
                  <li key={c.slug} className="animate-fade-up" style={{ animationDelay: `${index * 40}ms` }}>
                    <Link
                      to={categoryHref(c)}
                      onClick={close}
                      className={
                        c.group === 'sale'
                          ? 'inline-flex h-10 items-center rounded-full bg-brand px-5 text-sm font-medium text-brand-foreground transition-opacity hover:opacity-90'
                          : 'inline-flex h-10 items-center rounded-full border px-5 text-sm font-medium transition-colors hover:border-foreground hover:bg-foreground hover:text-background'
                      }
                    >
                      {c.label}
                    </Link>
                  </li>
                ))}
              </ul>
              <RecentlyViewed className="mt-10" onNavigate={close} />
            </div>
          ) : results.length === 0 ? (
            !isFetching && (
              <p className="mt-8 text-muted-foreground">
                Nothing matches “{query}” yet — try a shorter word or browse a room instead.
              </p>
            )
          ) : (
            <div className="mt-8">
              <div className="flex items-baseline justify-between gap-4">
                <p className="eyebrow">
                  {data?.totalCount} {data?.totalCount === 1 ? 'product' : 'products'}
                </p>
                <button type="button" onClick={() => showAll()} className="group inline-flex items-center gap-2 text-sm font-medium">
                  <span className="link-underline">See all results</span>
                  <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-1" />
                </button>
              </div>
              <ul className="mt-4 grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
                {results.map((product, index) => (
                  <li key={product.id} className="animate-fade-up" style={{ animationDelay: `${index * 50}ms` }}>
                    <Link
                      to={`/products/${product.id}`}
                      onClick={() => {
                        openProduct(product);
                        close();
                      }}
                      className="group flex items-center gap-4 rounded-2xl p-2 transition-colors hover:bg-accent"
                    >
                      <div className="h-16 w-20 shrink-0 overflow-hidden rounded-xl bg-muted">
                        <img src={product.image} alt="" className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-110" />
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="eyebrow">{product.company}</p>
                        <p className="truncate font-medium">{product.title}</p>
                      </div>
                      <span className="shrink-0 pr-2 text-sm">
                        <ProductPrice product={product} stacked />
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      </SheetContent>
    </Sheet>
  );
};

export default SearchDialog;
