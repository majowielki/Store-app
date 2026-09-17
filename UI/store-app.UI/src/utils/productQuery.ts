import type { ProductQuery } from '@/api/types';

const text = (params: URLSearchParams, key: string): string | undefined => params.get(key) || undefined;

/** The catalogue query the page's URL describes; "layout" and anything unknown stay out of the request. */
export const productQueryFrom = (params: URLSearchParams): ProductQuery => ({
  search: text(params, 'search'),
  group: text(params, 'group'),
  category: text(params, 'category'),
  company: text(params, 'company'),
  color: text(params, 'color'),
  order: text(params, 'order'),
  price: text(params, 'price'),
  sale: text(params, 'sale'),
  page: Number(params.get('page')) || undefined,
});
