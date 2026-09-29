import { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { http } from 'msw';
import { review, reviewSummary } from '@/test/fixtures';
import { api, json, page } from '@/test/handlers';
import ProductReviews from './ProductReviews';
import Stars, { RatingLine, StarsInput } from './Stars';

const reviews = [
  review(),
  review({
    id: 'b7a1c2d3-0000-4000-8000-000000000002',
    authorName: 'Tom W.',
    rating: 4,
    title: 'Worth the wait',
    body: 'Took a week longer than promised, but the finish is even and the legs sit flat on an old floor.',
    createdAt: '2026-07-14T08:30:00Z',
  }),
  review({
    id: 'b7a1c2d3-0000-4000-8000-000000000003',
    authorName: 'Ola K.',
    rating: 5,
    title: null,
    body: 'Exactly as in the pictures. We oiled it once after a month, as the care card says.',
    verifiedPurchase: false,
    createdAt: '2026-06-02T19:05:00Z',
  }),
];

/** The reviews section of a product page, with the API answering three published reviews. */
const meta = {
  title: 'Reviews/ProductReviews',
  component: ProductReviews,
  args: { productId: 7, productTitle: 'Oak Table' },
  parameters: {
    msw: {
      handlers: {
        overrides: [
          http.get(api('/reviews'), () => json(page(reviews))),
          http.get(api('/reviews/products/:id/summary'), () =>
            json(reviewSummary({ averageRating: 4.7, reviewCount: 3, distribution: { '1': 0, '2': 0, '3': 0, '4': 1, '5': 2 } })),
          ),
        ],
      },
    },
  },
  render: (args) => (
    <div className="w-[60rem]">
      <ProductReviews {...args} />
    </div>
  ),
} satisfies Meta<typeof ProductReviews>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Published: Story = {};

/** No review yet: the section invites the first one. */
export const NoReviews: Story = {
  parameters: {
    msw: {
      handlers: {
        overrides: [
          http.get(api('/reviews'), () => json(page([]))),
          http.get(api('/reviews/products/:id/summary'), () => json(reviewSummary({ averageRating: 0, reviewCount: 0, distribution: { '1': 0, '2': 0, '3': 0, '4': 0, '5': 0 } }))),
        ],
      },
    },
  },
};

const StarsPicker = () => {
  const [value, setValue] = useState(4);
  return <StarsInput value={value} onChange={setValue} />;
};

/** The stars on their own: a rating read out as "Rated 4.5 out of 5", the card's line and the picker of the form. */
export const Stars_: Story = {
  name: 'Stars',
  render: () => (
    <div className="grid gap-6">
      <Stars rating={4.5} size="lg" />
      <RatingLine average={4.7} count={23} />
      <StarsPicker />
    </div>
  ),
};
