import { Link, useParams } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { useGetLookbookQuery } from '@/api/content';
import ContentUnavailable from '@/components/content/ContentUnavailable';
import LookbookPicture, { AddLookButton } from '@/components/content/LookbookPicture';
import Loading from '@/components/Loading';
import ProductsGrid from '@/components/ProductsGrid';
import { useProductsBySlug } from '@/hooks/use-products-by-slug';

/** A lookbook: the room with a point on every piece, the pieces below and the whole look in one go. */
const LookPage = () => {
  const { slug = '' } = useParams<{ slug: string }>();
  const { data: look, isLoading, error } = useGetLookbookQuery(slug);
  const { products, bySlug } = useProductsBySlug(look?.hotspots.map((point) => point.productSlug));

  if (isLoading) return <Loading />;
  if (!look) return <ContentUnavailable error={error} what="Look" back={{ to: '/looks', label: 'All looks' }} />;

  return (
    <article>
      <Link to="/looks" className="group inline-flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-foreground">
        <ArrowLeft className="h-4 w-4 transition-transform group-hover:-translate-x-0.5" />
        All looks
      </Link>
      <header className="mt-8 flex flex-wrap items-end justify-between gap-6">
        <div>
          <p className="eyebrow animate-fade-up">Shop the look</p>
          <h1 className="display mt-4 animate-fade-up text-[clamp(2.5rem,6vw,5rem)] leading-[0.95] [animation-delay:80ms]">{look.title}</h1>
          {look.summary && <p className="mt-4 max-w-xl text-lg text-muted-foreground">{look.summary}</p>}
        </div>
        <AddLookButton products={products} />
      </header>
      <LookbookPicture image={look.image} alt={look.title} hotspots={look.hotspots} products={bySlug} className="mt-10" priority />
      <p className="mt-4 text-sm text-muted-foreground">Select a point on the picture to see the piece.</p>
      {products.length > 0 && (
        <section className="mt-20" aria-label="The pieces of this look">
          <h2 className="display border-b pb-6 text-4xl">In this room</h2>
          <div className="mt-10">
            <ProductsGrid products={products} />
          </div>
        </section>
      )}
    </article>
  );
};

export default LookPage;
