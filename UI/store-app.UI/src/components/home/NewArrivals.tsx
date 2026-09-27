import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useGetProductsQuery } from '@/api/catalog';
import ProductCard, { ProductPrice } from '@/components/ProductCard';
import Reveal from '@/components/Reveal';
import { Skeleton } from '@/components/ui/skeleton';
import type { Product } from '@/utils';

/** The first new arrival, large: the picture fills its half of the section. */
const Lead = ({ product }: { product: Product }) => (
  <Link to={`/products/${product.id}`} className="group relative block h-full min-h-104 overflow-hidden rounded-4xl bg-muted">
    <img src={product.image} alt={product.title} className="absolute inset-0 h-full w-full object-cover transition-transform [transition-duration:1200ms] ease-smooth group-hover:scale-105" />
    <div className="absolute inset-0 bg-linear-to-t from-black/60 via-transparent to-transparent" />
    <div className="absolute inset-x-0 bottom-0 flex items-end justify-between gap-4 p-6 text-white md:p-8">
      <div>
        <p className="text-[0.65rem] uppercase tracking-[0.2em] text-white/70">New · {product.company}</p>
        <p className="display mt-2 text-3xl md:text-5xl">{product.title}</p>
        <p className="mt-2 text-white/90">
          <ProductPrice product={product} />
        </p>
      </div>
      <span className="grid h-12 w-12 shrink-0 place-items-center rounded-full bg-white/15 backdrop-blur-md transition-colors duration-500 group-hover:bg-white group-hover:text-black">
        <ArrowUpRight className="h-5 w-5 transition-transform duration-500 ease-smooth group-hover:rotate-45" />
      </span>
    </div>
  </Link>
);

/** What just came in: one piece large on the left, four smaller ones beside it. */
const NewArrivals = () => {
  const { data, isLoading } = useGetProductsQuery({ newArrival: 'true', pageSize: 5 });
  const [lead, ...rest] = data?.items ?? [];

  return (
    <section className="align-element py-20 md:py-28" aria-labelledby="new-arrivals">
      <Reveal as="header" className="flex flex-wrap items-end justify-between gap-6">
        <div>
          <p className="eyebrow">Just in</p>
          <h2 id="new-arrivals" className="display mt-3 text-4xl md:text-6xl">
            New arrivals, <em className="text-brand">fresh from the workshop.</em>
          </h2>
        </div>
        <Link to="/products?newArrival=on" className="group inline-flex items-center gap-2 text-sm font-medium">
          <span className="link-underline">All {data?.totalCount ?? ''} new pieces</span>
          <ArrowUpRight className="h-4 w-4 transition-transform duration-300 group-hover:rotate-45" />
        </Link>
      </Reveal>
      <div className="mt-12 grid gap-6 lg:grid-cols-2">
        {isLoading || !lead ? (
          <>
            <Skeleton className="min-h-104 rounded-4xl" />
            <div className="grid grid-cols-2 gap-6">
              {Array.from({ length: 4 }, (_, index) => (
                <Skeleton key={index} className="aspect-5/4 rounded-2xl" />
              ))}
            </div>
          </>
        ) : (
          <>
            <Reveal>
              <Lead product={lead} />
            </Reveal>
            <div className="grid grid-cols-2 gap-x-6 gap-y-10">
              {rest.map((product, index) => (
                <Reveal key={product.id} delay={index * 90}>
                  <ProductCard product={product} />
                </Reveal>
              ))}
            </div>
          </>
        )}
      </div>
    </section>
  );
};

export default NewArrivals;
