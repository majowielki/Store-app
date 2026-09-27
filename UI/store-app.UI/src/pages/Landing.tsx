import { Hero } from '@/components';
import JournalTeaser from '@/components/home/JournalTeaser';
import LookbookFeature from '@/components/home/LookbookFeature';
import MakersStrip from '@/components/home/MakersStrip';
import NewArrivals from '@/components/home/NewArrivals';
import WelcomeBand from '@/components/home/WelcomeBand';
import RoomsGrid from '@/components/RoomsGrid';

/**
 * The home page, each kind of content once: what is new, a room to shop from, the rooms of the
 * catalogue, the makers, the journal, and one band with the welcome discount and the perks.
 */
const Landing = () => (
  <>
    <Hero />
    <NewArrivals />
    <LookbookFeature />
    <RoomsGrid />
    <MakersStrip />
    <JournalTeaser />
    <WelcomeBand />
  </>
);
export default Landing;
