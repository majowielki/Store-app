import type { Article, ProductDetail, StockAvailability } from '@/api/types';
import { SHOP_CURRENCY } from '@/utils/formatAsDollars';
import { absoluteUrl, SITE_LOGO, SITE_NAME } from './site';

// schema.org descriptions of the shop's pages, the way Google's rich results read them: the
// shop itself on the home page, a product with its offer and rating, an article, and the trail
// of links above a page. Search engines get absolute addresses under the shop's public one.

export type StructuredData = Record<string, unknown>;

const CONTEXT = 'https://schema.org';

/** A step of the trail above a page: its name and its address in the shop. */
export interface Crumb {
  name: string;
  path: string;
}

/** Where every trail starts. */
const HOME: Crumb = { name: 'Home', path: '/' };

/** How schema.org names the catalogue's availability. */
const AVAILABILITY: Record<StockAvailability, string> = {
  inStock: 'https://schema.org/InStock',
  lowStock: 'https://schema.org/LimitedAvailability',
  outOfStock: 'https://schema.org/OutOfStock',
};

const shop = (): StructuredData => ({ '@type': 'Organization', name: SITE_NAME, url: absoluteUrl('/'), logo: absoluteUrl(SITE_LOGO) });

/** The shop as an organisation and as a web site, for the home page. */
export const shopData = (): StructuredData[] => [
  { '@context': CONTEXT, ...shop() },
  { '@context': CONTEXT, '@type': 'WebSite', name: SITE_NAME, url: absoluteUrl('/') },
];

/** The trail of links from the home page (added here) to the page, the last step being the page itself. */
export const breadcrumbData = (trail: Crumb[]): StructuredData => ({
  '@context': CONTEXT,
  '@type': 'BreadcrumbList',
  itemListElement: [HOME, ...trail].map((crumb, index) => ({ '@type': 'ListItem', position: index + 1, name: crumb.name, item: absoluteUrl(crumb.path) })),
});

/** "modenza" as the maker's name, "Modenza". */
const brandName = (company: string) => company.charAt(0).toUpperCase() + company.slice(1);

/** A product with the price it sells at, whether it can be bought, and its rating once it has reviews. */
export const productData = (product: ProductDetail, path: string): StructuredData => ({
  '@context': CONTEXT,
  '@type': 'Product',
  name: product.title,
  description: product.description,
  image: [product.image, ...product.images.map((image) => image.url)],
  sku: product.slug,
  brand: { '@type': 'Brand', name: brandName(product.company) },
  ...(product.materials.length > 0 && { material: product.materials.join(', ') }),
  offers: {
    '@type': 'Offer',
    url: absoluteUrl(path),
    price: product.effectivePrice.toFixed(2),
    priceCurrency: SHOP_CURRENCY,
    availability: AVAILABILITY[product.availability],
    itemCondition: 'https://schema.org/NewCondition',
  },
  ...(product.ratingCount > 0 && {
    aggregateRating: { '@type': 'AggregateRating', ratingValue: product.ratingAverage, reviewCount: product.ratingCount },
  }),
});

/** A journal article, its author and the shop as its publisher. */
export const articleData = (article: Article, path: string): StructuredData => ({
  '@context': CONTEXT,
  '@type': 'Article',
  headline: article.title,
  description: article.excerpt,
  image: [article.coverImage],
  datePublished: article.publishedAt,
  dateModified: article.updatedAt,
  author: { '@type': 'Person', name: article.author },
  publisher: shop(),
  mainEntityOfPage: absoluteUrl(path),
});
