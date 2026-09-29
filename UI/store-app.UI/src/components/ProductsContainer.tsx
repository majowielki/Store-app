import { Link, useSearchParams } from 'react-router-dom';
import { LayoutGrid, List, SearchX } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { ProductsResponse } from '@/utils';
import ProductsGrid from './ProductsGrid';
import ProductsList from './ProductsList';
import { Button } from './ui/button';

const layoutButton = (active: boolean) =>
  cn(
    'grid h-8 w-8 place-items-center rounded-full transition-colors [&_svg]:h-4 [&_svg]:w-4',
    active ? 'bg-foreground text-background' : 'text-muted-foreground hover:text-foreground',
  );

const ProductsContainer = ({ page }: { page: ProductsResponse }) => {
  const [searchParams, setSearchParams] = useSearchParams();
  const currentLayout = searchParams.get('layout') || 'grid';
  const totalProducts = page.totalCount;

  const setLayout = (layout: 'grid' | 'list') => {
    const next = new URLSearchParams(searchParams);
    next.set('layout', layout);
    setSearchParams(next, { replace: true, preventScrollReset: true });
  };

  return (
    <>
      <div className="mb-8 flex items-center justify-between gap-4">
        <h2 className="text-sm text-muted-foreground">
          {totalProducts} product{totalProducts !== 1 && 's'}
        </h2>
        <div className="flex items-center gap-1 rounded-full border p-1" role="group" aria-label="Layout">
          <button type="button" onClick={() => setLayout('grid')} className={layoutButton(currentLayout === 'grid')} aria-label="Grid view" aria-pressed={currentLayout === 'grid'}>
            <LayoutGrid />
          </button>
          <button type="button" onClick={() => setLayout('list')} className={layoutButton(currentLayout === 'list')} aria-label="List view" aria-pressed={currentLayout === 'list'}>
            <List />
          </button>
        </div>
      </div>
      {totalProducts === 0 ? (
        <div className="grid place-items-center rounded-3xl border border-dashed px-6 py-20 text-center">
          <span className="grid h-14 w-14 place-items-center rounded-full bg-secondary">
            <SearchX className="h-6 w-6" />
          </span>
          <h5 className="display mt-6 text-3xl">Sorry, no products matched your search.</h5>
          <p className="mt-2 max-w-sm text-sm text-muted-foreground">Try fewer filters or other words — or start again from everything we make.</p>
          <Button asChild variant="outline" className="mt-8">
            <Link to="/products">Clear all filters</Link>
          </Button>
        </div>
      ) : currentLayout === 'grid' ? (
        <ProductsGrid products={page.items} />
      ) : (
        <ProductsList products={page.items} />
      )}
    </>
  );
};
export default ProductsContainer;
