import { Link, useSearchParams } from 'react-router-dom';
import { Package } from 'lucide-react';
import { useGetMyOrdersQuery } from '@/api/orders';
import { ComplexPaginationContainer, Loading, OrdersList, SectionTitle } from '@/components';
import { Button } from '@/components/ui/button';
import { usePageMeta } from '@/seo';

const Orders = () => {
  usePageMeta({ title: 'Your orders', noindex: true });
  const [searchParams] = useSearchParams();
  const page = Number(searchParams.get('page')) || 1;
  const { data: orders, isLoading } = useGetMyOrdersQuery({ page, pageSize: 20 });

  if (isLoading || !orders) return <Loading />;
  if (orders.totalCount < 1) {
    return (
      <div className="grid place-items-center py-24 text-center">
        <span className="grid h-20 w-20 place-items-center rounded-full bg-secondary">
          <Package className="h-8 w-8" />
        </span>
        <h1 className="display mt-8 text-5xl">No orders yet</h1>
        <p className="mt-3 text-muted-foreground">Once you place an order, you will find it here.</p>
        <Button asChild size="lg" className="mt-8">
          <Link to="/products">Start shopping</Link>
        </Button>
      </div>
    );
  }

  return (
    <>
      <SectionTitle eyebrow="Your account" text="Your Orders" />
      <OrdersList orders={orders} />
      <ComplexPaginationContainer page={orders.page} totalPages={orders.totalPages} />
    </>
  );
};
export default Orders;
