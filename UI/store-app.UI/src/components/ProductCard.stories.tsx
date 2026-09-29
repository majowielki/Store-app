import type { Meta, StoryObj } from '@storybook/react-vite';
import { armchair, coffeeTable, lamp, sofa } from '@/stories/data';
import ProductCard from './ProductCard';

/** The tile of the grid listings: picture, badges, maker, title, rating, price and the colours. */
const meta = {
  title: 'Catalogue/ProductCard',
  component: ProductCard,
  args: { product: coffeeTable, priority: true },
  render: (args) => (
    <div className="w-80">
      <ProductCard {...args} />
    </div>
  ),
} satisfies Meta<typeof ProductCard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

/** A new piece on sale: the sale and "New" badges, the old price struck through. */
export const NewOnSale: Story = { args: { product: sofa } };

/** Two left and no reviews yet: the stock badge, no rating line. */
export const LowStockNoReviews: Story = { args: { product: lamp } };

export const SoldOut: Story = { args: { product: armchair } };

export const Grid: Story = {
  render: () => (
    <div className="grid w-[64rem] grid-cols-4 gap-6">
      {[coffeeTable, sofa, lamp, armchair].map((product) => (
        <ProductCard key={product.id} product={product} priority />
      ))}
    </div>
  ),
};

export const Dark: Story = { globals: { theme: 'dark' }, args: { product: sofa } };
