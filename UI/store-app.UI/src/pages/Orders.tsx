import { useSearchParams } from 'react-router-dom';
import { useGetMyOrdersQuery } from '@/api/orders';
import { ComplexPaginationContainer, Loading, OrdersList, SectionTitle } from '@/components';

const Orders = () => {
  const [searchParams] = useSearchParams();
  const page = Number(searchParams.get('page')) || 1;
  const { data: orders, isLoading } = useGetMyOrdersQuery({ page, pageSize: 20 });

  if (isLoading || !orders) return <Loading />;
  if (orders.totalCount < 1) return <SectionTitle text="Please make an order" />;

  return (
    <>
      <SectionTitle text="Your Orders" />
      <OrdersList orders={orders} />
      <ComplexPaginationContainer page={orders.page} totalPages={orders.totalPages} />
    </>
  );
};
export default Orders;
