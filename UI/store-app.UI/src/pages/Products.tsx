import { useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { emptyProductsMeta, useGetProductsMetaQuery, useGetProductsQuery } from '@/api/catalog';
import { Filters, Loading, PaginationContainer, ProductsContainer } from '@/components';
import ActiveFilters from '@/components/ActiveFilters';
import CatalogHeader from '@/components/CatalogHeader';
import { productQueryFrom } from '@/utils/productQuery';

const Products = () => {
  const [searchParams] = useSearchParams();
  const query = useMemo(() => productQueryFrom(searchParams), [searchParams]);
  const { data: page, isLoading } = useGetProductsQuery(query);
  // The filter values are a separate resource; the page still renders without them
  const { data: meta = emptyProductsMeta } = useGetProductsMetaQuery();

  return (
    <>
      <CatalogHeader meta={meta} query={query} />
      <div className="mt-10 grid gap-8 md:grid-cols-[13rem_1fr] md:gap-10 lg:grid-cols-[16rem_1fr] lg:gap-14">
        <Filters meta={meta} query={query} />
        <div className="min-w-0">
          <ActiveFilters meta={meta} />
          {isLoading || !page ? (
            <Loading />
          ) : (
            <>
              <ProductsContainer page={page} />
              <PaginationContainer page={page.page} totalPages={page.totalPages} />
            </>
          )}
        </div>
      </div>
    </>
  );
};
export default Products;
