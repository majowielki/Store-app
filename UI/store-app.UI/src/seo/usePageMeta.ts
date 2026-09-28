import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { absoluteUrl, pageTitle, SITE_DESCRIPTION, SITE_IMAGE, SITE_IMAGE_ALT, SITE_TITLE } from './site';

/** What kind of page it is to social sites (og:type). */
export type PageKind = 'website' | 'product' | 'article';

export interface PageMetaProps {
  /** The page's own title, the shop's name follows it; none for the home page. */
  title?: string;
  /** One or two sentences for the search result; the shop's own when missing. */
  description?: string;
  /** The picture social sites show, absolute or from the root of the shop; the shop's when missing. */
  image?: string;
  imageAlt?: string;
  kind?: PageKind;
  /** Pages no search engine should list: the cart, the checkout, a customer's account, the admin panel. */
  noindex?: boolean;
}

/** Search results show about this many characters of a description; a longer one is cut at a word. */
const DESCRIPTION_MAX = 160;

const clip = (text: string) => {
  const plain = text.replace(/\s+/g, ' ').trim();
  if (plain.length <= DESCRIPTION_MAX) return plain;
  const cut = plain.slice(0, DESCRIPTION_MAX - 1);
  const lastSpace = cut.lastIndexOf(' ');
  return `${lastSpace > 0 ? cut.slice(0, lastSpace) : cut}…`;
};

/** The robots rule of a page that must not be listed; its links may still be followed. */
const NOINDEX = 'noindex, follow';
/** The picture's size is only known for the shop's own (public/og-image.jpg). */
const SITE_IMAGE_SIZE = { width: '1200', height: '630' };

type MetaKey = { name: string } | { property: string };

const selectorOf = (key: MetaKey) => ('name' in key ? `meta[name="${key.name}"]` : `meta[property="${key.property}"]`);

/** Writes a meta tag of the document's head, or removes it for null. */
const setMeta = (key: MetaKey, content: string | null) => {
  let element = document.head.querySelector<HTMLMetaElement>(selectorOf(key));
  if (content === null) {
    element?.remove();
    return;
  }
  if (!element) {
    element = document.createElement('meta');
    if ('name' in key) element.name = key.name;
    else element.setAttribute('property', key.property);
    document.head.appendChild(element);
  }
  element.content = content;
};

/** Writes the canonical link, or removes it for null. */
const setCanonical = (href: string | null) => {
  let element = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
  if (href === null) {
    element?.remove();
    return;
  }
  if (!element) {
    element = document.createElement('link');
    element.rel = 'canonical';
    document.head.appendChild(element);
  }
  element.href = href;
};

const apply = (meta: PageMetaProps, canonical: string | null) => {
  const title = pageTitle(meta.title);
  const description = clip(meta.description ?? '') || SITE_DESCRIPTION;
  const ownImage = Boolean(meta.image);
  document.title = title;
  setMeta({ name: 'description' }, description);
  setMeta({ name: 'robots' }, meta.noindex ? NOINDEX : null);
  setCanonical(meta.noindex ? null : canonical);
  setMeta({ property: 'og:title' }, meta.title ?? SITE_TITLE);
  setMeta({ property: 'og:description' }, description);
  setMeta({ property: 'og:type' }, meta.kind ?? 'website');
  setMeta({ property: 'og:url' }, canonical);
  setMeta({ property: 'og:image' }, absoluteUrl(meta.image || SITE_IMAGE));
  setMeta({ property: 'og:image:alt' }, meta.image ? (meta.imageAlt ?? meta.title ?? null) : SITE_IMAGE_ALT);
  setMeta({ property: 'og:image:width' }, ownImage ? null : SITE_IMAGE_SIZE.width);
  setMeta({ property: 'og:image:height' }, ownImage ? null : SITE_IMAGE_SIZE.height);
};

/**
 * The page's title, description, canonical address and social card, written into the document's
 * head while the page is shown; the shop's own come back when it goes (a page without this hook
 * never inherits the previous page's). The canonical address is the path without the query, so a
 * filtered listing counts as the listing.
 */
export const usePageMeta = ({ title, description, image, imageAlt, kind, noindex }: PageMetaProps) => {
  const { pathname } = useLocation();

  useEffect(() => {
    apply({ title, description, image, imageAlt, kind, noindex }, absoluteUrl(pathname));
    return () => apply({}, null);
  }, [title, description, image, imageAlt, kind, noindex, pathname]);
};
