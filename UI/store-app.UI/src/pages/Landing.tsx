import { Hero, FeaturedProducts } from '@/components';
import { LandingImageBox } from '@/components/LandingImageBox';
import InfoTiles from '@/components/InfoTiles';
import NewsletterSection from '@/components/NewsletterSection';

const Landing = () => (
  <>
    <Hero />
    <LandingImageBox />
    <FeaturedProducts />
    <InfoTiles />
    <NewsletterSection />
  </>
);
export default Landing;
