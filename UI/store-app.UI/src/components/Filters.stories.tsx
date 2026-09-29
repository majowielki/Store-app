import type { Meta, StoryObj } from '@storybook/react-vite';
import type { ProductsMeta } from '@/api/types';
import { productQueryFrom } from '@/utils/productQuery';
import ActiveFilters from './ActiveFilters';
import Filters from './Filters';

/** What the catalogue can be narrowed by, with how many pieces each choice leaves. */
const catalogueMeta: ProductsMeta = {
  categories: ['sofas', 'chairs', 'tables', 'tableLamps'],
  groups: ['furniture'],
  companies: ['modenza', 'luxora', 'artifex'],
  colors: ['white', 'brown', 'gray', 'teal'],
  groupCategoryMap: [
    {
      key: 'furniture',
      name: 'Furniture',
      categories: [
        { key: 'sofas', name: 'Sofas' },
        { key: 'chairs', name: 'Chairs' },
        { key: 'tables', name: 'Tables' },
        { key: 'tableLamps', name: 'Table lamps' },
      ],
    },
  ],
  counts: {
    total: 24,
    categories: { sofas: 6, chairs: 8, tables: 7, tableLamps: 3 },
    groups: { furniture: 24 },
    companies: { modenza: 9, luxora: 8, artifex: 7 },
    colors: { white: 7, brown: 11, gray: 4, teal: 2 },
    sale: 5,
    newArrival: 3,
  },
};

const route = '/products?group=furniture&category=tables&color=brown&sale=on&order=low';

/** The filter column of the catalogue on a wide screen, filled from the address. */
const meta = {
  title: 'Catalogue/Filters',
  component: Filters,
  args: { meta: catalogueMeta, query: productQueryFrom(new URLSearchParams(route.split('?')[1])) },
  parameters: { route },
  render: (args) => (
    <div className="w-72">
      <Filters {...args} />
    </div>
  ),
} satisfies Meta<typeof Filters>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Column: Story = {};

/** The chips above the listing: each one takes its filter away. */
export const ActiveChips: Story = {
  render: (args) => <ActiveFilters meta={args.meta} />,
};
