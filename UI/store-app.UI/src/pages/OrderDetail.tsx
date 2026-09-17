import { useParams } from 'react-router-dom';
import { useGetOrderQuery } from '@/api/orders';
import { Loading, OrderSummary, SectionTitle } from '@/components';

const OrderDetail = () => {
  const { id } = useParams<{ id: string }>();
  const { data: order, isLoading } = useGetOrderQuery(Number(id));

  if (isLoading) return <Loading />;
  if (!order) return <SectionTitle text="Order not found" />;
  return <OrderSummary title="Your order" order={order} />;
};

export default OrderDetail;
