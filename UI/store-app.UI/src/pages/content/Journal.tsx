import { useGetArticlesQuery } from '@/api/content';
import ContentCard from '@/components/content/ContentCard';
import Loading from '@/components/Loading';
import Reveal from '@/components/Reveal';
import SectionTitle from '@/components/SectionTitle';
import { PageMeta } from '@/seo';
import { formatDate } from '@/utils';

/** The journal, newest article first. */
const Journal = () => {
  const { data: articles, isLoading } = useGetArticlesQuery();

  return (
    <section>
      <PageMeta title="Journal" description="Ideas for living: care guides, advice on choosing furniture and stories from real rooms." />
      <SectionTitle eyebrow="Journal" text="Ideas for living" />
      {isLoading ? (
        <Loading />
      ) : (
        <div className="mt-12 grid gap-x-8 gap-y-16 md:grid-cols-2 xl:grid-cols-3">
          {articles?.map((article, index) => (
            <Reveal key={article.id} delay={(index % 3) * 90}>
              <ContentCard
                to={`/journal/${article.slug}`}
                image={article.coverImage}
                eyebrow={formatDate(article.publishedAt)}
                title={article.title}
                text={article.excerpt}
              />
            </Reveal>
          ))}
        </div>
      )}
    </section>
  );
};

export default Journal;
