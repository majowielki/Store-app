import type { Meta, StoryObj } from '@storybook/react-vite';
import { storyArticle, storyCollection } from '@/stories/data';
import { formatDate } from '@/utils';
import ContentCard from './ContentCard';

/** The card that leads to an editorial page: a collection, a journal article, a maker, a look. */
const meta = {
  title: 'Content/ContentCard',
  component: ContentCard,
  args: {
    to: `/collections/${storyCollection.slug}`,
    image: storyCollection.coverImage,
    eyebrow: `${storyCollection.productSlugs.length} pieces`,
    title: storyCollection.title,
    text: storyCollection.summary,
  },
  render: (args) => (
    <div className="w-[26rem]">
      <ContentCard {...args} />
    </div>
  ),
} satisfies Meta<typeof ContentCard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Collection: Story = {};

/** The first card of a listing takes the full width, with a wider picture. */
export const Featured: Story = {
  args: { wide: true, headingLevel: 2 },
  render: (args) => (
    <div className="w-[52rem]">
      <ContentCard {...args} />
    </div>
  ),
};

export const Article: Story = {
  args: {
    to: `/journal/${storyArticle.slug}`,
    image: storyArticle.coverImage,
    eyebrow: formatDate(storyArticle.publishedAt),
    title: storyArticle.title,
    text: storyArticle.excerpt,
  },
};
