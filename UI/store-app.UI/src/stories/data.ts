import { http } from 'msw';
import type { CartLine } from '@/features/cart/useCart';
import type { Finish, Product } from '@/api/types';
import { article, collection, order, product } from '@/test/fixtures';
import { api, json } from '@/test/handlers';

/**
 * What the stories show: real pieces of the catalogue with their pictures, which Storybook serves
 * from Blobs/w400 at /pictures (.storybook/main.ts), and the finishes they come in. The shapes are
 * the unit tests' fixtures, so they follow the API contract.
 */
export const picture = (name: string) => `/pictures/${name}.webp`;

export const storyFinishes: Finish[] = [
  { key: 'natural-oak', name: 'Natural oak', family: 'brown', swatch: [{ color: '#cca883', texture: 'naturalOak' }] },
  { key: 'walnut', name: 'Walnut', family: 'brown', swatch: [{ color: '#b98c74', texture: 'walnut' }] },
  { key: 'travertine', name: 'Travertine', family: 'white', swatch: [{ color: '#bba084', texture: 'travertine' }] },
  { key: 'rattan', name: 'Rattan', family: 'brown', swatch: [{ color: '#a48367', texture: 'rattan' }] },
  { key: 'cream-boucle', name: 'Cream bouclé', family: 'white', swatch: [{ color: '#d7cfc4' }] },
  { key: 'pebble-boucle', name: 'Pebble bouclé', family: 'gray', swatch: [{ color: '#b9b3a9' }] },
  { key: 'teal-walnut', name: 'Teal and walnut', family: 'teal', swatch: [{ color: '#4e6972' }, { color: '#b98c74', texture: 'walnut' }] },
  { key: 'linen-oak', name: 'Linen and oak', family: 'white', swatch: [{ color: '#cdc6b7' }, { color: '#cca883', texture: 'naturalOak' }] },
  { key: 'white-glaze-linen', name: 'White glaze and linen', family: 'white', swatch: [{ color: '#d0cac2', texture: 'whiteGlaze' }, { color: '#c2ac97' }] },
  { key: 'terracotta-linen', name: 'Terracotta and linen', family: 'orange', swatch: [{ color: '#875637', texture: 'terracotta' }, { color: '#c2ac97' }] },
  { key: 'cream-rust', name: 'Cream and rust', family: 'orange', swatch: [{ color: '#e1d5cb' }, { color: '#8e462e' }] },
  { key: 'cream-sage', name: 'Cream and sage', family: 'green', swatch: [{ color: '#e1d5cb' }, { color: '#a7ae98' }] },
  { key: 'navy-linen', name: 'Navy linen', family: 'navy', swatch: [{ color: '#364358' }] },
];

/** The stories' finishes in place of the unit tests' three; the first matching handler answers. */
export const storyHandlers = [http.get(api('/products/finishes'), () => json(storyFinishes))];

export const coffeeTable: Product = product({
  id: 1,
  title: 'Oak Plank Coffee Table',
  slug: 'oak-plank-coffee-table',
  company: 'modenza',
  category: 'tables',
  image: picture('OakPlankCoffeeTable-1'),
  price: 649,
  salePrice: null,
  effectivePrice: 649,
  colors: ['natural-oak', 'walnut'],
  ratingAverage: 4.7,
  ratingCount: 23,
});

export const sofa: Product = product({
  id: 2,
  title: 'Bouclé Modular Sofa',
  slug: 'boucle-modular-sofa',
  company: 'luxora',
  category: 'sofas',
  image: picture('BoucleModularSofa-1'),
  price: 2490,
  salePrice: 1990,
  effectivePrice: 1990,
  colors: ['cream-boucle', 'pebble-boucle'],
  newArrival: true,
  ratingAverage: 4.9,
  ratingCount: 41,
});

export const lamp: Product = product({
  id: 3,
  title: 'Ceramic Table Lamp',
  slug: 'ceramic-table-lamp',
  company: 'artifex',
  category: 'tableLamps',
  image: picture('CeramicTableLamp-1'),
  price: 189,
  salePrice: null,
  effectivePrice: 189,
  colors: ['white-glaze-linen', 'terracotta-linen'],
  availability: 'lowStock',
  availableQuantity: 2,
  ratingAverage: 0,
  ratingCount: 0,
});

export const armchair: Product = product({
  id: 4,
  title: 'Mid-Century Accent Chair',
  slug: 'mid-century-accent-chair',
  company: 'modenza',
  category: 'chairs',
  image: picture('MidCenturyAccentChair-1'),
  price: 590,
  salePrice: null,
  effectivePrice: 590,
  colors: ['teal-walnut'],
  availability: 'outOfStock',
  availableQuantity: 0,
  ratingAverage: 4.2,
  ratingCount: 9,
});

/** A bag of two lines, as useCart hands them to the components. */
export const cartLines: CartLine[] = [
  { key: '1-natural-oak', productId: 1, title: coffeeTable.title, image: coffeeTable.image, company: 'modenza', color: 'natural-oak', unitPrice: 649, quantity: 1, lineTotal: 649 },
  { key: '5-cream-rust', productId: 5, title: 'Linen Cushion Cover Set', image: picture('LinenCushionCoverSet-1'), company: 'artifex', color: 'cream-rust', unitPrice: 59, quantity: 2, lineTotal: 118 },
];

export const storyCollection = collection({ title: 'Warm Minimal', summary: 'Oak, linen and stone: fewer pieces, all of them warm.', coverImage: picture('Collection-WarmMinimal') });
export const storyArticle = article({ title: 'Caring for oak', excerpt: 'Oil it twice a year, and keep it out of the midday sun.', coverImage: picture('Journal-CaringForOak') });
export const paidOrder = order({
  status: 'Paid',
  statusHistory: [
    { status: 'Placed', changedAt: '2026-09-20T09:12:00Z' },
    { status: 'AwaitingPayment', changedAt: '2026-09-20T09:12:02Z' },
    { status: 'Paid', changedAt: '2026-09-20T09:14:30Z' },
  ],
  cardBrand: 'visa',
  cardLast4: '4242',
});

/** A store whose guest cart holds the lines above, for the components that read the cart. */
export const withGuestCart = {
  guestCart: { items: cartLines.map(({ productId, title, image, company, color, unitPrice, quantity }) => ({ productId, title, image, company, color, unitPrice, quantity })) },
};
