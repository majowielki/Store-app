import { useParams } from 'react-router-dom';
import { useGetAdminOrderQuery } from '@/api/orders';
import OrderSummary from '@/components/OrderSummary';

const OrderDetail = () => {
  const { id } = useParams<{ id: string }>();
  const { data: order, isLoading } = useGetAdminOrderQuery(Number(id));

  if (isLoading) return <div>Loading...</div>;
  if (!order) return <div>Order not found.</div>;
  return <OrderSummary title={`Order #${order.id}`} order={order} showStatus />;
};

export default OrderDetail;
