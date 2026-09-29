import type { Meta, StoryObj } from '@storybook/react-vite';
import { ArrowRight, Heart, ShoppingBag } from 'lucide-react';
import { Button } from './button';

const meta = {
  title: 'Primitives/Button',
  component: Button,
  args: { children: 'Add to bag' },
} satisfies Meta<typeof Button>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

/** Every look the shop uses, side by side: the black default, the terracotta call to action and the quiet ones. */
export const Variants: Story = {
  render: () => (
    <div className="flex flex-wrap items-center gap-3">
      <Button>
        <ShoppingBag />
        Add to bag
      </Button>
      <Button variant="brand">
        Shop the sale
        <ArrowRight />
      </Button>
      <Button variant="outline">View bag</Button>
      <Button variant="secondary">Secondary</Button>
      <Button variant="ghost">Ghost</Button>
      <Button variant="link">Link</Button>
      <Button variant="destructive">Remove</Button>
      <Button variant="ghost" size="icon" aria-label="Add to wishlist">
        <Heart />
      </Button>
    </div>
  ),
};

export const Sizes: Story = {
  render: () => (
    <div className="flex flex-wrap items-center gap-3">
      <Button size="sm">Small</Button>
      <Button>Default</Button>
      <Button size="lg">Large</Button>
    </div>
  ),
};

export const Disabled: Story = { args: { disabled: true } };

export const Dark: Story = { ...Variants, globals: { theme: 'dark' } };
