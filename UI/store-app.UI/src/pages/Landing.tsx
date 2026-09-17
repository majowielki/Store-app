import { Hero, FeaturedProducts } from "@/components";
import { LandingImageBox } from "@/components/LandingImageBox";
import InfoTiles from '@/components/InfoTiles';
import NewsletterSection from '@/components/NewsletterSection';
import { newsletterApi, productApi } from '@/utils/api';
import { type LoaderFunction } from "react-router-dom";
import { type ProductsResponse } from "@/utils";

// eslint-disable-next-line react-refresh/only-export-components
export const loader: LoaderFunction = async (): Promise<ProductsResponse> =>
  productApi.getProducts({ sale: 'true' });

const Landing = () => {
  return (
    <>
  <Hero />
  <LandingImageBox />
  <FeaturedProducts />
  <InfoTiles />
  <NewsletterSection onSubscribe={newsletterApi.subscribe} />
    </>
  );
}
export default Landing;
