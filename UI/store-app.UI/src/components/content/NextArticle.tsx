import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useGetArticlesQuery } from '@/api/content';
import ResponsiveImage from '@/components/ResponsiveImage';
import Reveal from '@/components/Reveal';

/**
 * The end of an article: the next one in the journal (the first again after the last), as a
 * large cover to turn to, the way a book's next chapter waits on the facing page.
 */
const NextArticle = ({ currentSlug }: { currentSlug: string }) => {
  const { data: articles } = useGetArticlesQuery();
  if (!articles || articles.length < 2) return null;
  const index = articles.findIndex((article) => article.slug === currentSlug);
  const next = articles[(index + 1) % articles.length];

  return (
    <Reveal as="section" aria-label="Read next" className="mt-24 border-t pt-12">
      <Link to={`/journal/${next.slug}`} viewTransition className="group grid gap-8 md:grid-cols-12 md:items-end">
        <div className="md:col-span-5">
          <p className="eyebrow">Read next</p>
          <p className="display mt-3 text-4xl leading-[1.02] md:text-5xl">{next.title}</p>
          {next.excerpt && <p className="mt-4 max-w-md leading-relaxed text-muted-foreground">{next.excerpt}</p>}
          <span className="mt-6 inline-flex items-center gap-2 text-sm font-medium">
            <span className="link-underline">Turn the page</span>
            <ArrowUpRight className="h-4 w-4 transition-transform duration-300 group-hover:rotate-45" aria-hidden />
          </span>
        </div>
        <div className="aspect-3/2 overflow-hidden rounded-4xl bg-muted md:col-span-7">
          <ResponsiveImage
            size="half"
            placeholder
            loading="lazy"
            src={next.coverImage}
            alt=""
            className="h-full w-full object-cover transition-transform [transition-duration:1200ms] ease-smooth group-hover:scale-[1.04]"
          />
        </div>
      </Link>
    </Reveal>
  );
};

export default NextArticle;
