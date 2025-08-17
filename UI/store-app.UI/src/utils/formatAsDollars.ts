export const formatAsDollars = (price: string | number): string => {
  const n = typeof price === 'string' ? Number(price) : price;
  const value = Number.isFinite(n) ? n : 0;
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(value);
};
