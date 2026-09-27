import { useParams } from 'react-router-dom';
import { useGetArticleQuery } from '@/api/content';
import ContentHero from '@/components/content/ContentHero';
import ContentUnavailable from '@/components/content/ContentUnavailable';
import Markdown from '@/components/content/Markdown';
import Loading from '@/components/Loading';
import ProductsGrid from '@/components/ProductsGrid';
import { useProductsBySlug } from '@/hooks/use-products-by-slug';
import { formatDate } from '@/utils';

/** A journal article and, under it, the products it talks about. */
const ArticlePage = () => {
  const { slug = '' } = useParams<{ slug: string }>();
  const { data: article, isLoading, error } = useGetArticleQuery(slug);
  const { products } = useProductsBySlug(article?.productSlugs);

  if (isLoading) return <Loading />;
  if (!article) return <ContentUnavailable error={error} what="Article" back={{ to: '/journal', label: 'The journal' }} />;

  return (
    <article>
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
      <div className="mx-auto mt-16 max-w-2xl">
        <Markdown>{article.body}</Markdown>
      </div>
      {products.length > 0 && (
        <section className="mt-24" aria-label="Products in this article">
          <h2 className="display border-b pb-6 text-4xl">Shop the story</h2>
          <div className="mt-10">
            <ProductsGrid products={products} />
          </div>
        </section>
      )}
    </article>
  );
};

export default ArticlePage;
