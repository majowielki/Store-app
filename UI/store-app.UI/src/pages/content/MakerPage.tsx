import { Link, useParams } from 'react-router-dom';
import { ArrowRight } from 'lucide-react';
import { useGetProductsQuery } from '@/api/catalog';
import { useGetMakerQuery } from '@/api/content';
import ContentHero from '@/components/content/ContentHero';
import ContentUnavailable from '@/components/content/ContentUnavailable';
import Markdown from '@/components/content/Markdown';
import Loading from '@/components/Loading';
import ProductsGrid from '@/components/ProductsGrid';
import { breadcrumbData, JsonLd, PageMeta } from '@/seo';

/** A maker's story and the products the catalogue has from them. */
const MakerPage = () => {
  const { slug = '' } = useParams<{ slug: string }>();
  const { data: maker, isLoading, error } = useGetMakerQuery(slug);
  const { data: products } = useGetProductsQuery({ company: maker?.company, pageSize: 6 }, { skip: !maker });

  if (isLoading) return <Loading />;
  if (!maker) return <ContentUnavailable error={error} what="Maker" back={{ to: '/products', label: 'Back to the shop' }} />;

  return (
    <article>
      <PageMeta title={maker.name} description={maker.tagline} image={maker.coverImage} />
      <JsonLd data={breadcrumbData([{ name: 'Makers', path: '/makers' }, { name: maker.name, path: `/makers/${maker.slug}` }])} />
      <ContentHero
        eyebrow="Our makers"
        title={maker.name}
        lead={maker.tagline}
        image={maker.coverImage}
        back={{ to: '/products', label: 'Shop' }}
        meta={
          <span>
            {maker.location}
            {maker.foundedYear ? ` · since ${maker.foundedYear}` : ''}
          </span>
        }
      />
      <div className="mx-auto mt-16 max-w-2xl">
        <Markdown>{maker.story}</Markdown>
      </div>
      {products && products.items.length > 0 && (
        <section className="mt-24">
          <div className="flex items-end justify-between gap-6 border-b pb-6">
            <h2 className="display text-4xl">From {maker.name}</h2>
            <Link to={`/products?company=${maker.company}`} className="group inline-flex items-center gap-2 text-sm font-medium">
              <span className="link-underline">All {products.totalCount} pieces</span>
              <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-1" />
            </Link>
          </div>
          <div className="mt-10">
            <ProductsGrid products={products.items} />
          </div>
        </section>
      )}
    </article>
  );
};

export default MakerPage;
