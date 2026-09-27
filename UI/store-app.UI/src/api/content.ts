import { api, unlessFailed } from './api';
import type {
  Article,
  ArticlePayload,
  Collection,
  CollectionPayload,
  Lookbook,
  LookbookPayload,
  Maker,
  MakerPayload,
} from './types';

/** The four kinds of content, as the API names them in its paths. */
export type ContentKind = 'makers' | 'collections' | 'articles' | 'lookbooks';

interface ContentTypes {
  makers: { entry: Maker; payload: MakerPayload };
  collections: { entry: Collection; payload: CollectionPayload };
  articles: { entry: Article; payload: ArticlePayload };
  lookbooks: { entry: Lookbook; payload: LookbookPayload };
}

export type ContentEntry<K extends ContentKind> = ContentTypes[K]['entry'];
export type ContentPayload<K extends ContentKind> = ContentTypes[K]['payload'];

/**
 * Makers, collections, journal articles and lookbooks. The shop reads the published entries
 * (the service answers an unchanged page with 304, which the browser serves from its cache);
 * the admin panel lists everything and saves through the admin endpoints.
 */
export const contentApi = api.injectEndpoints({
  endpoints: (build) => ({
    getMakers: build.query<Maker[], void>({ query: () => '/content/makers', providesTags: ['Content'] }),
    getMaker: build.query<Maker, string>({ query: (slug) => `/content/makers/${slug}`, providesTags: ['Content'] }),
    getCollections: build.query<Collection[], void>({ query: () => '/content/collections', providesTags: ['Content'] }),
    getCollection: build.query<Collection, string>({ query: (slug) => `/content/collections/${slug}`, providesTags: ['Content'] }),
    getArticles: build.query<Article[], void>({ query: () => '/content/articles', providesTags: ['Content'] }),
    getArticle: build.query<Article, string>({ query: (slug) => `/content/articles/${slug}`, providesTags: ['Content'] }),
    getLookbooks: build.query<Lookbook[], void>({ query: () => '/content/lookbooks', providesTags: ['Content'] }),
    getLookbook: build.query<Lookbook, string>({ query: (slug) => `/content/lookbooks/${slug}`, providesTags: ['Content'] }),

    getContentAdmin: build.query<ContentEntry<ContentKind>[], ContentKind>({
      query: (kind) => `/content/admin/${kind}`,
      providesTags: ['Content'],
    }),
    getContentEntryAdmin: build.query<ContentEntry<ContentKind>, { kind: ContentKind; id: number }>({
      query: ({ kind, id }) => `/content/admin/${kind}/${id}`,
      providesTags: ['Content'],
    }),
    createContent: build.mutation<ContentEntry<ContentKind>, { kind: ContentKind; body: ContentPayload<ContentKind> }>({
      query: ({ kind, body }) => ({ url: `/content/admin/${kind}`, method: 'POST', body }),
      invalidatesTags: unlessFailed(['Content']),
    }),
    updateContent: build.mutation<ContentEntry<ContentKind>, { kind: ContentKind; id: number; body: ContentPayload<ContentKind> }>({
      query: ({ kind, id, body }) => ({ url: `/content/admin/${kind}/${id}`, method: 'PUT', body }),
      invalidatesTags: unlessFailed(['Content']),
    }),
    deleteContent: build.mutation<void, { kind: ContentKind; id: number }>({
      query: ({ kind, id }) => ({ url: `/content/admin/${kind}/${id}`, method: 'DELETE' }),
      invalidatesTags: unlessFailed(['Content']),
    }),
  }),
});

export const {
  useGetMakersQuery,
  useGetMakerQuery,
  useGetCollectionsQuery,
  useGetCollectionQuery,
  useGetArticlesQuery,
  useGetArticleQuery,
  useGetLookbooksQuery,
  useGetLookbookQuery,
  useGetContentAdminQuery,
  useGetContentEntryAdminQuery,
  useCreateContentMutation,
  useUpdateContentMutation,
  useDeleteContentMutation,
} = contentApi;
