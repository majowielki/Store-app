import type { Meta, StoryObj } from '@storybook/react-vite';
import { storyFinishes } from '@/stories/data';
import Swatch from './Swatch';

/** A finish the way the product pictures show it: a colour, a surface, or two split on the diagonal. */
const meta = {
  title: 'Catalogue/Swatch',
  component: Swatch,
  args: { color: 'natural-oak', className: 'h-10 w-10' },
} satisfies Meta<typeof Swatch>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Wood: Story = {};

export const TwoParts: Story = { args: { color: 'teal-walnut' } };

/** A colour an older cart kept, not a finish of the catalogue: drawn as the CSS colour of that name. */
export const PlainColour: Story = { args: { color: 'olive' } };

export const EveryFinish: Story = {
  render: () => (
    <ul className="grid grid-cols-4 gap-4">
      {storyFinishes.map((finish) => (
        <li key={finish.key} className="flex items-center gap-3 text-sm">
          <Swatch color={finish.key} className="h-9 w-9" />
          {finish.name}
        </li>
      ))}
    </ul>
  ),
};
