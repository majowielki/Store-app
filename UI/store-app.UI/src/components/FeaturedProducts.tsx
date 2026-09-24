import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useGetProductsQuery } from '@/api/catalog';
import { Carousel, CarouselContent, CarouselItem, CarouselNext, CarouselPrevious } from '@/components/ui/carousel';
import { Skeleton } from '@/components/ui/skeleton';
import { priceTag } from '@/utils';
import ProductCard from './ProductCard';
import Reveal from './Reveal';

/** The products on sale (topped up with others when there are few) in a carousel. */
const FeaturedProducts = () => {
  const { data, isLoading } = useGetProductsQuery({ sale: 'true' });
  const products = data?.items ?? [];
  const discounted = products.filter((product) => priceTag(product).hasSale);
  let toShow = discounted.slice(0, 8);
  if (toShow.length < 4) {
    toShow = toShow.concat(products.filter((p) => !discounted.includes(p)).slice(0, 4 - toShow.length));
  }

  return (
    <section className="overflow-hidden border-y bg-card/60 py-20 md:py-28">
      <Carousel opts={{ align: 'start' }} className="align-element">
        <Reveal as="header" className="flex flex-wrap items-end justify-between gap-6">
          <div>
            <p className="eyebrow">On sale now</p>
            <h2 className="display mt-3 text-4xl md:text-6xl">
              Good things, <em className="text-brand">for less.</em>
            </h2>
          </div>
          <div className="flex items-center gap-4">
            <Link to="/products?sale=on" className="group mr-2 inline-flex items-center gap-2 text-sm font-medium">
              <span className="link-underline">View the sale</span>
              <ArrowUpRight className="h-4 w-4 transition-transform duration-300 group-hover:rotate-45" />
            </Link>
            <CarouselPrevious variant="outline" className="static h-11 w-11 translate-y-0" />
            <CarouselNext variant="outline" className="static h-11 w-11 translate-y-0" />
          </div>
        </Reveal>
        <CarouselContent className="-ml-6 mt-12">
          {isLoading
            ? Array.from({ length: 4 }, (_, index) => (
                <CarouselItem key={index} className="basis-[80%] pl-6 sm:basis-1/2 lg:basis-1/3 xl:basis-1/4">
                  <Skeleton className="aspect-[5/4] rounded-2xl" />
                  <Skeleton className="mt-4 h-4 w-2/3" />
                </CarouselItem>
              ))
            : toShow.map((product, index) => (
                <CarouselItem key={product.id} className="basis-[80%] pl-6 sm:basis-1/2 lg:basis-1/3 xl:basis-1/4">
                  <Reveal delay={Math.min(index, 3) * 90}>
                    <ProductCard product={product} />
                  </Reveal>
                </CarouselItem>
              ))}
        </CarouselContent>
      </Carousel>
    </section>
  );
};
export default FeaturedProducts;
