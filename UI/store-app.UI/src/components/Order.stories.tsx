import type { Meta, StoryObj } from '@storybook/react-vite';
import { order } from '@/test/fixtures';
import { paidOrder } from '@/stories/data';
import CheckoutSteps from './CheckoutSteps';
import OrderTimeline from './OrderTimeline';

/** Where a purchase stands: the steps of the checkout and the history of an order. */
const meta = {
  title: 'Checkout/Progress',
  component: CheckoutSteps,
  args: { current: 2 },
} satisfies Meta<typeof CheckoutSteps>;

export default meta;
type Story = StoryObj<typeof meta>;

/** The payment step: the bag and the delivery done, the confirmation still ahead. */
export const CheckoutAtPayment: Story = {};

export const CheckoutDone: Story = { args: { current: 4 } };

/** A paid order: placed, paid with the card named, the shipping still to come. */
export const PaidOrder: Story = {
  render: () => (
    <div className="w-[28rem]">
      <OrderTimeline order={paidOrder} />
    </div>
  ),
};

export const CancelledOrder: Story = {
  render: () => (
    <div className="w-[28rem]">
      <OrderTimeline
        order={order({
          status: 'Cancelled',
          cancellationReason: 'payment-timed-out',
          statusHistory: [
            { status: 'Placed', changedAt: '2026-09-20T09:12:00Z' },
            { status: 'AwaitingPayment', changedAt: '2026-09-20T09:12:02Z' },
            { status: 'Cancelled', changedAt: '2026-09-20T09:27:02Z' },
          ],
        })}
      />
    </div>
  ),
};
