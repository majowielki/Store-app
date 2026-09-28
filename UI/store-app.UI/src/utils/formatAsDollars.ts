/** The currency the API's amounts are in, and the locale the shop writes numbers in: the shop is in English and prices in dollars. */
export const SHOP_CURRENCY = 'USD';
export const SHOP_LOCALE = 'en-US';

const money = new Intl.NumberFormat(SHOP_LOCALE, { style: 'currency', currency: SHOP_CURRENCY });

export const formatAsDollars = (price: string | number): string => {
  const n = typeof price === 'string' ? Number(price) : price;
  const value = Number.isFinite(n) ? n : 0;
  return money.format(value);
};
