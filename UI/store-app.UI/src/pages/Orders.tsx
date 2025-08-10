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
      const response = await customFetch.get<OrdersResponse>('/orders/my-orders', {
        params,
      });
      return response.data;
    } catch (error) {
      console.log(error);
      toast({ description: 'Failed to fetch orders' });
      // Return safe empty response to avoid runtime null errors
      const empty: OrdersResponse = {
        orders: [],
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