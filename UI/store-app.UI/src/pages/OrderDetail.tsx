import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { orderApi } from '@/utils/api';
import type { Order } from '@/utils/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { formatAsDollars } from '@/utils';

const OrderDetail = () => {
  const { id } = useParams<{ id: string }>();
  const [order, setOrder] = useState<Order | null>(null);

  useEffect(() => {
    const load = async () => {
      if (!id) return;
      const o = await orderApi.getOrder(Number(id));
      setOrder(o);
    };
    void load();
  }, [id]);

  if (!order) return <div>Loading...</div>;

  // Extract delivery and discount from orderItems
  const deliveryItem = order.orderItems.find((it) => it.deliveryCost != null);
  const discountItem = order.orderItems.find((it) => it.orderDiscount != null);

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle>Your order</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid md:grid-cols-2 gap-4 text-sm">
            <div>
              <div>Name: {order.customerName}</div>
              <div>Email: {order.userEmail}</div>
              {order.deliveryAddress && <div>Address: {order.deliveryAddress}</div>}
              <div>Date: {new Date(order.createdAt).toLocaleString()}</div>
            </div>
            <div>
              <div>Total Items: {order.totalItems}</div>
              <div>Order Total: {formatAsDollars(order.orderTotal)}</div>
              {deliveryItem && (
                <div>Delivery: {formatAsDollars(deliveryItem.deliveryCost ?? 0)}</div>
              )}
              {discountItem && (
                <div>Order Discount: -{formatAsDollars(discountItem.orderDiscount ?? 0)}</div>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Items</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Product</TableHead>
                <TableHead>Color</TableHead>
                <TableHead>Qty</TableHead>
                <TableHead>Price</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {order.orderItems
                .filter((it) => it.deliveryCost == null && it.orderDiscount == null)
                .map((it) => (
                  <TableRow key={it.id}>
                    <TableCell className="flex items-center gap-2">
                      {it.productImage ? (
                        <img src={it.productImage} alt={it.productTitle} className="h-10 w-10 object-cover rounded" />
                      ) : null}
                      <span>{it.productTitle}</span>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <span className="inline-block h-4 w-4 rounded-full border" style={{ backgroundColor: it.color }} />
                        <span className="uppercase text-xs">{it.color}</span>
                      </div>
                    </TableCell>
                    <TableCell>{it.quantity}</TableCell>
                    <TableCell>{formatAsDollars(it.price)}</TableCell>
                    <TableCell className="text-right">
                      <Link to={`/products/${it.productId}`} className="text-sm text-primary hover:underline">View product</Link>
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
};

export default OrderDetail;
