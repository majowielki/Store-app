import { Hero, FeaturedProducts } from "@/components";
import { LandingImageBox } from "@/components/LandingImageBox";
import InfoTiles from '@/components/InfoTiles';
import NewsletterSection from '@/components/NewsletterSection';
import { newsletterApi } from '@/utils/api';
import { type LoaderFunction } from "react-router-dom";
import { customFetch, type ProductsResponse } from "@/utils";

const url = "/products";

// eslint-disable-next-line react-refresh/only-export-components
export const loader: LoaderFunction = async (): Promise<ProductsResponse> => {
  const response = await customFetch<ProductsResponse>(url, { params: { sale: true } });
  return { ...response.data };
};

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
