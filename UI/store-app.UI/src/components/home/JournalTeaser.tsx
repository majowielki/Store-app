import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useGetArticlesQuery } from '@/api/content';
import ContentCard from '@/components/content/ContentCard';
import Reveal from '@/components/Reveal';
import { formatDate } from '@/utils';

/** The three newest journal articles. */
const JournalTeaser = () => {
  const { data: articles } = useGetArticlesQuery();
  if (!articles?.length) return null;

  return (
    <section className="align-element py-20 md:py-28" aria-labelledby="journal">
      <Reveal as="header" className="flex flex-wrap items-end justify-between gap-6">
        <div>
          <p className="eyebrow">Journal</p>
          <h2 id="journal" className="display mt-3 text-4xl md:text-6xl">
            Ideas for <em>living well.</em>
          </h2>
        </div>
        <Link to="/journal" className="group inline-flex items-center gap-2 text-sm font-medium">
          <span className="link-underline">Read the journal</span>
          <ArrowUpRight className="h-4 w-4 transition-transform duration-300 group-hover:rotate-45" />
        </Link>
      </Reveal>
      <div className="mt-12 grid gap-x-8 gap-y-14 md:grid-cols-3">
        {articles.slice(0, 3).map((article, index) => (
          <Reveal key={article.id} delay={index * 90}>
            <ContentCard to={`/journal/${article.slug}`} image={article.coverImage} eyebrow={formatDate(article.publishedAt)} title={article.title} text={article.excerpt} />
          </Reveal>
        ))}
      </div>
    </section>
  );
};

export default JournalTeaser;
