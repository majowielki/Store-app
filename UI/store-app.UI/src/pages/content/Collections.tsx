import { useGetCollectionsQuery } from '@/api/content';
import ContentCard from '@/components/content/ContentCard';
import Loading from '@/components/Loading';
import Reveal from '@/components/Reveal';
import SectionTitle from '@/components/SectionTitle';

/** Every published collection; the first one gets the full width. */
const Collections = () => {
  const { data: collections, isLoading } = useGetCollectionsQuery();

  return (
    <section>
      <SectionTitle eyebrow="Curated" text="Collections" />
      {isLoading ? (
        <Loading />
      ) : (
        <div className="mt-12 grid gap-x-8 gap-y-16 md:grid-cols-2">
          {collections?.map((collection, index) => (
            <Reveal key={collection.id} delay={(index % 2) * 90} className={index === 0 ? 'md:col-span-2' : undefined}>
              <ContentCard
                to={`/collections/${collection.slug}`}
                image={collection.coverImage}
                eyebrow={`${collection.productSlugs.length} pieces`}
                title={collection.title}
                text={collection.summary}
                wide={index === 0}
              />
            </Reveal>
          ))}
        </div>
      )}
    </section>
  );
};

export default Collections;
