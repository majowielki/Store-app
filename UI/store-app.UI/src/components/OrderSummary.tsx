import { Link } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { formatAsDollars, type Order } from '@/utils';

interface OrderSummaryProps {
  title: string;
  order: Order;
  /** The admin views also show the order status. */
  showStatus?: boolean;
}

/** One order in full: the customer, the amounts and the lines. Shared by the customer's and the admin's view. */
const OrderSummary = ({ title, order, showStatus = false }: OrderSummaryProps) => (
  <div className="space-y-4">
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
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
            <div>Subtotal: {formatAsDollars(order.subtotal)}</div>
            {order.discountAmount > 0 && <div>Order Discount: -{formatAsDollars(order.discountAmount)}</div>}
            <div>Delivery: {formatAsDollars(order.deliveryFee)}</div>
            <div>Order Total: {formatAsDollars(order.total)}</div>
            {showStatus && <div>Status: {order.status}</div>}
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
            {order.orderItems.map((it) => (
              <TableRow key={it.id}>
                <TableCell className="flex items-center gap-2">
                  {it.productImage ? <img src={it.productImage} alt={it.productTitle} className="h-10 w-10 object-cover rounded" /> : null}
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
                  <Link to={`/products/${it.productId}`} className="text-sm text-primary hover:underline">
                    View product
                  </Link>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  </div>
);

export default OrderSummary;
