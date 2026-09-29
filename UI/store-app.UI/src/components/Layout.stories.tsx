import type { Meta, StoryObj } from '@storybook/react-vite';
import AnnouncementBar from './AnnouncementBar';
import Footer from './Footer';
import SectionTitle from './SectionTitle';

/** The parts every page shares: the ticker above the header, a page's title and the footer. */
const meta = {
  title: 'Layout/Page parts',
  component: SectionTitle,
  args: { eyebrow: 'Curated', text: 'Collections' },
} satisfies Meta<typeof SectionTitle>;

export default meta;
type Story = StoryObj<typeof meta>;

export const PageTitle: Story = {};

/** The ticker, with the button that stops it (the stories run with the motion stopped). */
export const AnnouncementBar_: Story = {
  name: 'Announcement bar',
  parameters: { layout: 'fullscreen' },
  render: () => <AnnouncementBar />,
};

export const Footer_: Story = {
  name: 'Footer',
  parameters: { layout: 'fullscreen' },
  render: () => <Footer />,
};
