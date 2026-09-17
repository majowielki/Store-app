import { useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { emptyProductsMeta, useGetProductsMetaQuery, useGetProductsQuery } from '@/api/catalog';
import { Filters, Loading, PaginationContainer, ProductsContainer } from '@/components';
import { productQueryFrom } from '@/utils/productQuery';

const Products = () => {
  const [searchParams] = useSearchParams();
  const query = useMemo(() => productQueryFrom(searchParams), [searchParams]);
  const { data: page, isLoading } = useGetProductsQuery(query);
  // The filter values are a separate resource; the page still renders without them
  const { data: meta = emptyProductsMeta } = useGetProductsMetaQuery();

  return (
    <>
      <Filters meta={meta} query={query} />
      {isLoading || !page ? (
        <Loading />
      ) : (
        <>
          <ProductsContainer page={page} />
          <PaginationContainer page={page.page} totalPages={page.totalPages} />
        </>
      )}
    </>
  );
};
export default Products;
