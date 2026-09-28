import { useParams } from 'react-router-dom';
import { useGetCollectionQuery } from '@/api/content';
import ContentHero from '@/components/content/ContentHero';
import ContentUnavailable from '@/components/content/ContentUnavailable';
import Markdown from '@/components/content/Markdown';
import Loading from '@/components/Loading';
import ProductsGrid from '@/components/ProductsGrid';
import { breadcrumbData, JsonLd, PageMeta } from '@/seo';
import { useProductsBySlug } from '@/hooks/use-products-by-slug';

/** A collection: its idea in a few paragraphs, then its products in the order it lists them. */
const CollectionPage = () => {
  const { slug = '' } = useParams<{ slug: string }>();
  const { data: collection, isLoading, error } = useGetCollectionQuery(slug);
  const { products } = useProductsBySlug(collection?.productSlugs);

  if (isLoading) return <Loading />;
  if (!collection) return <ContentUnavailable error={error} what="Collection" back={{ to: '/collections', label: 'All collections' }} />;

  return (
    <article>
      <PageMeta title={collection.title} description={collection.summary} image={collection.coverImage} />
      <JsonLd data={breadcrumbData([{ name: 'Collections', path: '/collections' }, { name: collection.title, path: `/collections/${collection.slug}` }])} />
      <ContentHero
        eyebrow="Collection"
        title={collection.title}
        lead={collection.summary}
        image={collection.coverImage}
        back={{ to: '/collections', label: 'All collections' }}
      />
      {collection.body && (
        <div className="mx-auto mt-16 max-w-2xl">
          <Markdown>{collection.body}</Markdown>
        </div>
      )}
      {products.length > 0 && (
        <section className="mt-24" aria-label={`Products in ${collection.title}`}>
          <h2 className="display border-b pb-6 text-4xl">In this collection</h2>
          <div className="mt-10">
            <ProductsGrid products={products} />
          </div>
        </section>
      )}
    </article>
  );
};

export default CollectionPage;
