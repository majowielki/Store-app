import type { ContentEntry, ContentKind, ContentPayload } from '@/api/content';

interface KindConfig {
  /** The admin menu and the list title. */
  label: string;
  /** "Add a {singular}". */
  singular: string;
  /** Where the shop shows a published entry. */
  publicPath: (slug: string) => string;
}

export const contentKinds: Record<ContentKind, KindConfig> = {
  makers: { label: 'Makers', singular: 'maker', publicPath: (slug) => `/makers/${slug}` },
  collections: { label: 'Collections', singular: 'collection', publicPath: (slug) => `/collections/${slug}` },
  articles: { label: 'Journal', singular: 'article', publicPath: (slug) => `/journal/${slug}` },
  lookbooks: { label: 'Lookbooks', singular: 'lookbook', publicPath: (slug) => `/looks/${slug}` },
};

export const isContentKind = (value: string | undefined): value is ContentKind =>
  value !== undefined && value in contentKinds;

/** The name an entry is listed under: a maker's name, anything else's title. */
export const entryTitle = (entry: ContentEntry<ContentKind>) => ('name' in entry ? entry.name : entry.title);

/** What a new entry starts with; unpublished, so a half-written entry never reaches the shop. */
export const emptyPayload = (kind: ContentKind): ContentPayload<ContentKind> => {
  switch (kind) {
    case 'makers':
      return { slug: '', name: '', company: 'modenza', tagline: '', story: '', location: '', foundedYear: null, coverImage: '', isPublished: false };
    case 'collections':
      return { slug: '', title: '', summary: '', body: '', coverImage: '', productSlugs: [], sortOrder: 0, isPublished: false };
    case 'articles':
      return { slug: '', title: '', excerpt: '', body: '', coverImage: '', author: 'The Store journal', publishedAt: null, productSlugs: [], isPublished: false };
    case 'lookbooks':
      return { slug: '', title: '', summary: '', image: '', hotspots: [], sortOrder: 0, isPublished: false };
  }
};

/** An entry as the form edits it: everything the API accepts back, without id and dates it sets itself. */
export const toPayload = (kind: ContentKind, entry: ContentEntry<ContentKind>): ContentPayload<ContentKind> => {
  switch (kind) {
    case 'makers': {
      if (!('name' in entry)) throw new Error("Unexpected content shape");
      const m = entry;
      return { slug: m.slug, name: m.name, company: m.company, tagline: m.tagline, story: m.story, location: m.location, foundedYear: m.foundedYear ?? null, coverImage: m.coverImage, isPublished: m.isPublished };
    }
    case 'collections': {
      if (!('summary' in entry && 'body' in entry)) throw new Error("Unexpected content shape");
      const c = entry;
      return { slug: c.slug, title: c.title, summary: c.summary, body: c.body, coverImage: c.coverImage, productSlugs: c.productSlugs, sortOrder: c.sortOrder, isPublished: c.isPublished };
    }
    case 'articles': {
      if (!('excerpt' in entry)) throw new Error("Unexpected content shape");
      const a = entry;
      return { slug: a.slug, title: a.title, excerpt: a.excerpt, body: a.body, coverImage: a.coverImage, author: a.author, publishedAt: a.publishedAt, productSlugs: a.productSlugs, isPublished: a.isPublished };
    }
    case 'lookbooks': {
      if (!('hotspots' in entry)) throw new Error("Unexpected content shape");
      const l = entry;
      return { slug: l.slug, title: l.title, summary: l.summary, image: l.image, hotspots: l.hotspots, sortOrder: l.sortOrder, isPublished: l.isPublished };
    }
  }
};

/** Lowercase words joined by dashes, folding accents - the address a title suggests. */
export const slugify = (text: string) =>
  text
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/ł/g, 'l')
    .replace(/Ł/g, 'L')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
