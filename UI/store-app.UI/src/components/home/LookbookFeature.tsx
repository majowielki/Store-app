import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useGetLookbooksQuery } from '@/api/content';
import LookbookPicture, { AddLookButton } from '@/components/content/LookbookPicture';
import Reveal from '@/components/Reveal';
import { useProductsBySlug } from '@/hooks/use-products-by-slug';

/** The first lookbook, with its points: a whole room to shop from the home page. */
const LookbookFeature = () => {
  const { data: looks } = useGetLookbooksQuery();
  const look = looks?.[0];
  const { products, bySlug } = useProductsBySlug(look?.hotspots.map((point) => point.productSlug));

  if (!look) return null;

  return (
    <section className="border-y bg-card/60 py-20 md:py-28" aria-labelledby="shop-the-look">
      <div className="align-element grid gap-10 lg:grid-cols-12 lg:items-end">
        <Reveal as="header" className="lg:col-span-4">
          <p className="eyebrow">Shop the look</p>
          <h2 id="shop-the-look" className="display mt-3 text-4xl md:text-6xl">
            {look.title}
          </h2>
          <p className="mt-5 text-lg leading-relaxed text-muted-foreground">{look.summary}</p>
          <p className="mt-3 text-sm text-muted-foreground">Select a point on the picture to see the piece.</p>
          <div className="mt-8 flex flex-wrap items-center gap-4">
            <AddLookButton products={products} />
            <Link to="/looks" className="group inline-flex items-center gap-2 text-sm font-medium">
              <span className="link-underline">More rooms</span>
              <ArrowUpRight className="h-4 w-4 transition-transform duration-300 group-hover:rotate-45" />
            </Link>
          </div>
        </Reveal>
        <Reveal delay={120} className="lg:col-span-8">
          <LookbookPicture image={look.image} alt={look.title} hotspots={look.hotspots} products={bySlug} size="half" />
        </Reveal>
      </div>
    </section>
  );
};

export default LookbookFeature;
