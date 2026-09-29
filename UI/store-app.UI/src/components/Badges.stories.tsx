import type { Meta, StoryObj } from '@storybook/react-vite';
import { armchair, coffeeTable, lamp, sofa } from '@/stories/data';
import ProductPrice from './ProductPrice';
import SaleBadge from './SaleBadge';
import StockBadge from './StockBadge';

/** What a product card and page say about the price and the stock. */
const meta = {
  title: 'Catalogue/Price and badges',
  component: ProductPrice,
  args: { product: coffeeTable },
} satisfies Meta<typeof ProductPrice>;

export default meta;
type Story = StoryObj<typeof meta>;

export const RegularPrice: Story = {};

export const SalePrice: Story = { args: { product: sofa } };

/** The card's layout: the sale price over the old one. */
export const SalePriceStacked: Story = { args: { product: sofa, stacked: true } };

/** On a photo, as the cards show them: the sale, the stock left and the sold-out badge. */
export const Badges: Story = {
  render: () => (
    <div className="flex items-center gap-2 rounded-2xl bg-muted p-4">
      <SaleBadge percent={20} />
      <StockBadge product={coffeeTable} />
      <StockBadge product={lamp} />
      <StockBadge product={armchair} />
    </div>
  ),
};
