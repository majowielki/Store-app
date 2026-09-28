import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { cn } from '@/lib/utils';
import OrderTimeline from './OrderTimeline';
import FinishLabel from './swatch/FinishLabel';
import { formatAsDollars, formatDateTime, type Order } from '@/utils';

interface OrderSummaryProps {
  title: string;
  order: Order;
  /** Buttons under the progress, for the admin view. */
  actions?: ReactNode;
  /** More for each line, before "View product" (the customer's "Write a review"). */
  lineActions?: (item: Order['orderItems'][number]) => ReactNode;
}

/** One "Label: value" line; the label and the value stay in one element, the way the page reads it. */
const Line = ({ label, value, strong = false }: { label: string; value: string; strong?: boolean }) => (
  <p className={cn('flex items-baseline justify-between gap-4 py-1.5', strong ? 'mt-2 border-t pt-3 text-base font-medium' : 'text-sm')}>
    <span className={strong ? undefined : 'text-muted-foreground'}>{label}:</span> <span className="text-right tabular-nums">{value}</span>
  </p>
);

/** One order in full: its progress, the customer, the amounts and the lines. Shared by the customer's and the admin's view. */
const OrderSummary = ({ title, order, actions, lineActions }: OrderSummaryProps) => (
  <div className="animate-fade-up">
    <header className="flex flex-wrap items-end justify-between gap-4 border-b pb-8">
      <div>
        <p className="eyebrow">Order #{order.id}</p>
        <h1 className="display mt-3 text-5xl md:text-6xl">{title}</h1>
      </div>
      <p className="text-sm text-muted-foreground">{formatDateTime(order.createdAt)}</p>
    </header>

    <div className="mt-10 grid items-start gap-8 lg:grid-cols-12">
      <div className="overflow-hidden rounded-2xl border bg-card lg:col-span-8">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
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
                <TableCell className="flex items-center gap-3 font-medium">
                  {it.productImage ? <img src={it.productImage} alt={it.productTitle} className="h-12 w-14 rounded-lg object-cover" /> : null}
                  <span>{it.productTitle}</span>
                </TableCell>
                <TableCell>
                  <FinishLabel color={it.color} className="text-xs text-muted-foreground" />
                </TableCell>
                <TableCell className="tabular-nums">{it.quantity}</TableCell>
                <TableCell className="tabular-nums">{formatAsDollars(it.price)}</TableCell>
                <TableCell className="text-right">
                  <div className="flex flex-wrap items-center justify-end gap-3">
                    {lineActions?.(it)}
                    <Link to={`/products/${it.productId}`} className="link-underline text-sm">
                      View product
                    </Link>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <aside className="grid gap-6 lg:col-span-4">
        <div className="grid gap-3">
          <OrderTimeline order={order} />
          {actions}
        </div>
        <div className="rounded-2xl border bg-card p-6">
          <h2 className="eyebrow mb-3">Delivery</h2>
          <Line label="Name" value={order.customerName} />
          <Line label="Email" value={order.userEmail} />
          {order.deliveryAddress && <Line label="Address" value={order.deliveryAddress} />}
          <Line label="Date" value={formatDateTime(order.createdAt)} />
        </div>
        <div className="rounded-2xl bg-secondary/60 p-6">
          <h2 className="eyebrow mb-3">Summary</h2>
          <Line label="Total Items" value={String(order.totalItems)} />
          <Line label="Subtotal" value={formatAsDollars(order.subtotal)} />
          {order.discountAmount > 0 && (
            <Line label={order.discountCode ? `Code ${order.discountCode}` : 'Order Discount'} value={`-${formatAsDollars(order.discountAmount)}`} />
          )}
          <Line label="Delivery" value={formatAsDollars(order.deliveryFee)} />
          <Line label="Order Total" value={formatAsDollars(order.total)} strong />
        </div>
      </aside>
    </div>
  </div>
);

export default OrderSummary;
