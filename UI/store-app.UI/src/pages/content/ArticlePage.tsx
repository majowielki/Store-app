import { useRef } from 'react';
import { useParams } from 'react-router-dom';
import { useGetArticleQuery } from '@/api/content';
import ContentHero from '@/components/content/ContentHero';
import ContentUnavailable from '@/components/content/ContentUnavailable';
import Markdown from '@/components/content/Markdown';
import NextArticle from '@/components/content/NextArticle';
import ReadingProgress from '@/components/content/ReadingProgress';
import Loading from '@/components/Loading';
import ProductsGrid from '@/components/ProductsGrid';
import { articleData, breadcrumbData, JsonLd, PageMeta } from '@/seo';
import { useProductsBySlug } from '@/hooks/use-products-by-slug';
import { formatDate } from '@/utils';

/**
 * A journal article set like a page of a book - a narrow column, a drop cap, a hairline that fills
 * as it is read - then the products it talks about and the next article to turn to.
 */
const ArticlePage = () => {
  const { slug = '' } = useParams<{ slug: string }>();
  const { data: article, isLoading, error } = useGetArticleQuery(slug);
  const { products } = useProductsBySlug(article?.productSlugs);
  const body = useRef<HTMLDivElement>(null);

  if (isLoading) return <Loading />;
  if (!article) return <ContentUnavailable error={error} what="Article" back={{ to: '/journal', label: 'The journal' }} />;
  const path = `/journal/${article.slug}`;

  return (
    <article>
      <PageMeta title={article.title} description={article.excerpt} image={article.coverImage} kind="article" />
      <JsonLd data={[articleData(article, path), breadcrumbData([{ name: 'Journal', path: '/journal' }, { name: article.title, path }])]} />
      <ContentHero
        eyebrow="Journal"
        title={article.title}
        lead={article.excerpt}
        image={article.coverImage}
        back={{ to: '/journal', label: 'The journal' }}
        meta={
          <span>
            <time dateTime={article.publishedAt}>{formatDate(article.publishedAt)}</time>
            {article.author ? ` · ${article.author}` : ''}
          </span>
        }
      />
      <ReadingProgress target={body} />
      <div ref={body} className="mx-auto mt-16 max-w-2xl">
        <Markdown className="drop-cap text-lg leading-8">{article.body}</Markdown>
      </div>
      {products.length > 0 && (
        <section className="mt-24" aria-label="Products in this article">
          <h2 className="display border-b pb-6 text-4xl">Shop the story</h2>
          <div className="mt-10">
            <ProductsGrid products={products} />
          </div>
        </section>
      )}
      <NextArticle currentSlug={article.slug} />
    </article>
  );
};

export default ArticlePage;
