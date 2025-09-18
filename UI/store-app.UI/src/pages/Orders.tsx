/* eslint-disable react-refresh/only-export-components */
import { LoaderFunction, redirect, useLoaderData } from 'react-router-dom';
import { toast } from '@/hooks/use-toast';
import { customFetch } from '@/utils';
import {
  OrdersList,
  ComplexPaginationContainer,
  SectionTitle,
} from '@/components';
import { ReduxStore } from '@/store';
import { type OrdersResponse } from '@/utils';

export const loader =
  (store: ReduxStore): LoaderFunction =>
  async ({ request }): Promise<OrdersResponse | Response | null> => {
    const user = store.getState().userState.user;

    if (!user) {
      toast({ description: 'Please login to continue' });
      return redirect('/login');
    }
    const params = Object.fromEntries([
      ...new URL(request.url).searchParams.entries(),
    ]);
    try {
      // Use user-specific endpoint; admin endpoint is protected
      const response = await customFetch.get('/orders/my-orders', {
        params,
      });
      // Map backend response to OrdersResponse shape expected by UI
      const backend = response.data.data;
      const items = backend.orders || [];
      const mapped: OrdersResponse = {
        items,
        totalCount: backend.totalCount ?? items.length,
        page: backend.page ?? 1,
        pageSize: backend.pageSize ?? 20,
        totalPages: backend.totalPages ?? 1,
        hasNextPage: backend.hasNextPage ?? false,
        hasPreviousPage: backend.hasPreviousPage ?? false,
      };
      return mapped;
  } catch {
      toast({ description: 'Failed to fetch orders' });
      // Return safe empty response to avoid runtime null errors
      const empty: OrdersResponse = {
        items: [],
        totalCount: 0,
        page: Number(params.page) || 1,
        pageSize: Number(params.pageSize) || 20,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false,
      };
      return empty;
    }
  };

const Orders = () => {
  const ordersResponse = useLoaderData() as OrdersResponse;
  if (!ordersResponse || ordersResponse.totalCount < 1) {
    return <SectionTitle text='Please make an order' />;
  }

  return (
    <>
      <SectionTitle text='Your Orders' />
      <OrdersList />
      <ComplexPaginationContainer />
    </>
  );
}
export default Orders;