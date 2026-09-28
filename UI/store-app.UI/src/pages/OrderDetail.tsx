import { Link, useParams } from 'react-router-dom';
import { useGetOrderQuery } from '@/api/orders';
import { Loading, OrderSummary, SectionTitle } from '@/components';
import { Button } from '@/components/ui/button';
import { formatDateTime } from '@/utils';

const OrderDetail = () => {
  const { id } = useParams<{ id: string }>();
  const { data: order, isLoading } = useGetOrderQuery(Number(id));

  if (isLoading) return <Loading />;
  if (!order) return <SectionTitle text="Order not found" />;

  // An order waiting for its payment can be paid from here until its deadline
  const payNow =
    order.status === 'AwaitingPayment' ? (
      <div className="grid gap-2 rounded-2xl border bg-card p-6">
        <p className="text-sm text-muted-foreground">
          Your pieces are held until {order.paymentDueAt ? formatDateTime(order.paymentDueAt) : 'the deadline'}.
        </p>
        <Button asChild>
          <Link to={`/orders/${order.id}/pay`}>Pay now</Link>
        </Button>
      </div>
    ) : undefined;

  return (
    <>
      <Link to="/orders" className="link-underline mb-8 inline-block text-sm text-muted-foreground hover:text-foreground">
        ← All orders
      </Link>
      <OrderSummary title="Your order" order={order} actions={payNow} />
    </>
  );
};

export default OrderDetail;
