import type { Meta, StoryObj } from '@storybook/react-vite';
import { cartLines, withGuestCart } from '@/stories/data';
import CartItemsList from './CartItemsList';
import CartTotals from './CartTotals';
import DiscountCodeField from './DiscountCodeField';
import FreeDeliveryProgress from './FreeDeliveryProgress';

/** The bag page's pieces: its lines, and the summary beside them. */
const meta = {
  title: 'Cart/Bag',
  component: CartItemsList,
  args: { lines: cartLines },
  parameters: { store: withGuestCart },
  render: (args) => (
    <div className="w-[40rem]">
      <CartItemsList {...args} />
    </div>
  ),
} satisfies Meta<typeof CartItemsList>;

export default meta;
type Story = StoryObj<typeof meta>;

/** Two lines: the picture, the maker and title, the finish, the amount and the line's total. */
export const Lines: Story = {};

/** The order summary of a visitor's bag: the subtotal, the delivery and the total. */
export const Summary: Story = {
  render: () => (
    <div className="w-96 rounded-3xl border bg-card p-6">
      <h2 className="display text-3xl">Order summary</h2>
      <FreeDeliveryProgress subtotal={767} />
      <DiscountCodeField className="mt-6" />
      <CartTotals className="mt-6" />
    </div>
  ),
};

/** Below the free delivery threshold: how much more makes the delivery free. */
export const DeliveryNotYetFree: Story = {
  render: () => (
    <div className="w-96">
      <FreeDeliveryProgress subtotal={189} />
    </div>
  ),
};
