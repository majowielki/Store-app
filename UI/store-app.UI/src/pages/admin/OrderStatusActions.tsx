import { useState } from 'react';
import { useChangeOrderStatusMutation } from '@/api/orders';
import ConfirmDialog from '@/components/ConfirmDialog';
import { Button } from '@/components/ui/button';
import { toast } from '@/hooks/use-toast';
import type { Order, OrderStatus } from '@/utils';

/** What the administrator does by hand; paying and refunding follow the payment on their own. */
type AdministratorMove = Extract<OrderStatus, 'Shipped' | 'Cancelled'>;

const buttons: Record<AdministratorMove, string> = {
  Shipped: 'Mark as shipped',
  Cancelled: 'Cancel order',
};

/**
 * The moves the order service allows from the order's status, one button each: ship a paid
 * order, cancel one not shipped yet (asking first). The demo administrator sees the buttons and
 * the service refuses the change.
 */
const OrderStatusActions = ({ order }: { order: Order }) => {
  const [changeStatus, { isLoading }] = useChangeOrderStatusMutation();
  const [confirmCancel, setConfirmCancel] = useState(false);
  const next = order.nextStatuses.filter((status): status is AdministratorMove => status in buttons);

  if (next.length === 0) return null;

  const move = async (status: AdministratorMove) => {
    try {
      await changeStatus({ id: order.id, status }).unwrap();
      toast({ description: `Order #${order.id} is now ${status.toLowerCase()}.` });
    } catch {
      // reported by the error middleware
    } finally {
      setConfirmCancel(false);
    }
  };

  return (
    <div className="flex flex-wrap gap-2">
      {next.map((status) =>
        status === 'Cancelled' ? (
          <Button key={status} type="button" variant="outline" disabled={isLoading} onClick={() => setConfirmCancel(true)}>
            {buttons[status]}
          </Button>
        ) : (
          <Button key={status} type="button" disabled={isLoading} onClick={() => move(status)}>
            {buttons[status]}
          </Button>
        ),
      )}
      <ConfirmDialog
        open={confirmCancel}
        title={`Cancel order #${order.id}?`}
        description={
          order.status === 'Paid'
            ? 'The customer sees the order as cancelled and the payment is refunded to their card. This cannot be undone.'
            : 'The customer sees the order as cancelled and its pieces go back on sale. This cannot be undone.'
        }
        confirmLabel="Cancel order"
        busy={isLoading}
        onConfirm={() => move('Cancelled')}
        onCancel={() => setConfirmCancel(false)}
      />
    </div>
  );
};

export default OrderStatusActions;
