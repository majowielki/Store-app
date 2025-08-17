import { Filters, ProductsContainer, PaginationContainer } from "@/components";
import {
  customFetch,
  type ProductsResponse,
  type ProductsResponseWithParams,
} from "../utils";
import { productApi } from '@/utils/api';
import { type LoaderFunction } from "react-router-dom";

const url = "/products";

// eslint-disable-next-line react-refresh/only-export-components
export const loader: LoaderFunction = async ({
  request,
}): Promise<ProductsResponseWithParams> => {
  const params = Object.fromEntries([
    ...new URL(request.url).searchParams.entries(),
  ]);

  const [productsRes, meta] = await Promise.all([
    customFetch<ProductsResponse>(url, { params }),
    productApi.getProductsMeta().catch(() => undefined),
  ]);

  // If meta returned, replace response meta (preserves pagination from productsRes)
  const merged: ProductsResponse = meta
    ? { ...productsRes.data, meta: { ...productsRes.data.meta, ...meta } }
    : productsRes.data;

  return { ...merged, params };
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
