import { useGetLookbooksQuery } from '@/api/content';
import ContentCard from '@/components/content/ContentCard';
import Loading from '@/components/Loading';
import Reveal from '@/components/Reveal';
import SectionTitle from '@/components/SectionTitle';

/** Every published lookbook. */
const Looks = () => {
  const { data: looks, isLoading } = useGetLookbooksQuery();

  return (
    <section>
      <SectionTitle eyebrow="Shop the look" text="Rooms to live in" />
      {isLoading ? (
        <Loading />
      ) : (
        <div className="mt-12 grid gap-x-8 gap-y-16 md:grid-cols-2">
          {looks?.map((look, index) => (
            <Reveal key={look.id} delay={(index % 2) * 90} className={index === 0 ? 'md:col-span-2' : undefined}>
              <ContentCard
                to={`/looks/${look.slug}`}
                image={look.image}
                eyebrow={`${look.hotspots.length} pieces`}
                title={look.title}
                text={look.summary}
                wide
              />
            </Reveal>
          ))}
        </div>
      )}
    </section>
  );
};

export default Looks;
