import { useSearchParams } from 'react-router-dom';
import { LayoutGrid, List } from 'lucide-react';
import type { ProductsResponse } from '@/utils';
import ProductsGrid from './ProductsGrid';
import ProductsList from './ProductsList';
import { Button } from './ui/button';
import { Separator } from './ui/separator';

const ProductsContainer = ({ page }: { page: ProductsResponse }) => {
  const [searchParams, setSearchParams] = useSearchParams();
  const currentLayout = searchParams.get('layout') || 'grid';
  const totalProducts = page.totalCount;

  const setLayout = (layout: 'grid' | 'list') => {
    const next = new URLSearchParams(searchParams);
    next.set('layout', layout);
    setSearchParams(next, { replace: true });
  };

  return (
    <>
      <section>
        <div className="flex justify-between items-center mt-8">
          <h4 className="font-medium text-md">
            {totalProducts} product{totalProducts !== 1 && 's'}
          </h4>
          <div className="flex gap-x-4">
            <Button onClick={() => setLayout('grid')} variant={currentLayout === 'grid' ? 'default' : 'ghost'} size="icon" aria-label="Grid view">
              <LayoutGrid />
            </Button>
            <Button onClick={() => setLayout('list')} variant={currentLayout === 'list' ? 'default' : 'ghost'} size="icon" aria-label="List view">
              <List />
            </Button>
          </div>
        </div>
        <Separator className="mt-4" />
      </section>
      <div>
        {totalProducts === 0 ? (
          <h5 className="text-2xl mt-16">Sorry, no products matched your search...</h5>
        ) : currentLayout === 'grid' ? (
          <ProductsGrid products={page.items} />
        ) : (
          <ProductsList products={page.items} />
        )}
      </div>
    </>
  );
};
export default ProductsContainer;
