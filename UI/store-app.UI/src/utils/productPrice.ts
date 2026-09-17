import type { Product } from './types';

/** What the price tag of a product shows: the list price, the price charged and the discount if any. */
export interface PriceTag {
  price: number;
  effectivePrice: number;
  hasSale: boolean;
  /** Discount in percent, rounded; 0 without a sale. */
  percent: number;
}

export const priceTag = (product: Pick<Product, 'price' | 'effectivePrice'>): PriceTag => {
  const { price, effectivePrice } = product;
  const hasSale = effectivePrice < price;
  const percent = hasSale && price > 0 ? Math.round(((price - effectivePrice) / price) * 100) : 0;
  return { price, effectivePrice, hasSale, percent };
};
