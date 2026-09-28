import { useGetMakersQuery } from '@/api/content';
import ContentCard from '@/components/content/ContentCard';
import Loading from '@/components/Loading';
import Reveal from '@/components/Reveal';
import SectionTitle from '@/components/SectionTitle';
import { PageMeta } from '@/seo';

/** The workshops behind the catalogue. */
const Makers = () => {
  const { data: makers, isLoading } = useGetMakersQuery();

  return (
    <section>
      <PageMeta title="Our makers" description="The workshops behind the catalogue: who makes each piece, where and how." />
      <SectionTitle eyebrow="Who makes it" text="Our makers" />
      {isLoading ? (
        <Loading />
      ) : (
        <div className="mt-12 grid gap-x-8 gap-y-16 md:grid-cols-2 xl:grid-cols-3">
          {makers?.map((maker, index) => (
            <Reveal key={maker.id} delay={(index % 3) * 90}>
              <ContentCard
                to={`/makers/${maker.slug}`}
                image={maker.coverImage}
                eyebrow={maker.location}
                title={maker.name}
                text={maker.tagline}
              />
            </Reveal>
          ))}
        </div>
      )}
    </section>
  );
};

export default Makers;
