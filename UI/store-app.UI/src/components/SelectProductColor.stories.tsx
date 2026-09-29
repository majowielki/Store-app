import { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import SelectProductColor from './SelectProductColor';

/** The colour picker of the product page: the chosen finish named above its swatches. */
const Picker = ({ colors }: { colors: string[] }) => {
  const [color, setColor] = useState(colors[0]);
  return <SelectProductColor colors={colors} productColor={color} setProductColor={setColor} />;
};

const meta = {
  title: 'Catalogue/SelectProductColor',
  component: Picker,
  args: { colors: ['white-glaze-linen', 'terracotta-linen'] },
} satisfies Meta<typeof Picker>;

export default meta;
type Story = StoryObj<typeof meta>;

export const TwoFinishes: Story = {};

export const ManyFinishes: Story = { args: { colors: ['cream-rust', 'cream-sage', 'navy-linen', 'natural-oak', 'walnut', 'rattan'] } };
