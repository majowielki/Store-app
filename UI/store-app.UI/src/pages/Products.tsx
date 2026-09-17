import { Filters, ProductsContainer, PaginationContainer } from "@/components";
import { emptyProductsMeta, type ProductsResponseWithParams } from "../utils";
import { productApi } from '@/utils/api';
import { type LoaderFunction } from "react-router-dom";

// eslint-disable-next-line react-refresh/only-export-components
export const loader: LoaderFunction = async ({
  request,
}): Promise<ProductsResponseWithParams> => {
  const params = Object.fromEntries([
    ...new URL(request.url).searchParams.entries(),
  ]);

  // The filter values are a separate resource; the page still renders without them
  const [page, meta] = await Promise.all([
    productApi.getProducts(params),
    productApi.getProductsMeta().catch(() => emptyProductsMeta),
  ]);

  return { ...page, meta, params };
};

const Products = () => {
  return (
    <>
      <Filters />
      <ProductsContainer />
      <PaginationContainer />
    </>
  );
}
export default Products;
