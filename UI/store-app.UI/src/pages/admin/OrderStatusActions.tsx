import { useState } from 'react';
import { useChangeOrderStatusMutation } from '@/api/orders';
import ConfirmDialog from '@/components/ConfirmDialog';
import { Button } from '@/components/ui/button';
import { toast } from '@/hooks/use-toast';
import type { Order, OrderStatus } from '@/utils';

const buttons: Record<Exclude<OrderStatus, 'Placed'>, string> = {
  Paid: 'Mark as paid',
  Shipped: 'Mark as shipped',
  Cancelled: 'Cancel order',
};

/**
 * The moves the order service allows from the order's status, one button each; cancelling
 * asks first. The demo administrator sees the buttons and the service refuses the change.
 */
const OrderStatusActions = ({ order }: { order: Order }) => {
  const [changeStatus, { isLoading }] = useChangeOrderStatusMutation();
  const [confirmCancel, setConfirmCancel] = useState(false);
  const next = order.nextStatuses as Exclude<OrderStatus, 'Placed'>[];

  if (next.length === 0) return null;

  const move = async (status: Exclude<OrderStatus, 'Placed'>) => {
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
        description="The customer sees the order as cancelled. This cannot be undone."
        confirmLabel="Cancel order"
        busy={isLoading}
        onConfirm={() => move('Cancelled')}
        onCancel={() => setConfirmCancel(false)}
      />
    </div>
  );
};

export default OrderStatusActions;
