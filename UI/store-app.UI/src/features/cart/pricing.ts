// The store's pricing rules as the cart page previews them. They mirror the order
// service's PricingOptions (delivery, free-delivery threshold, first-order discount);
// the order itself is always priced by the server. Reading them from the API instead
// of keeping a copy here is a separate step of the plan.
export const FREE_DELIVERY_THRESHOLD = 299;
export const DELIVERY_FEE = 10;
export const FIRST_ORDER_DISCOUNT_PERCENT = 20;

export interface CartPreview {
  subtotal: number;
  discount: number;
  deliveryFee: number;
  total: number;
}

/** The amounts the cart page shows; the delivery threshold is compared with the subtotal before the discount. */
export const previewTotals = (subtotal: number, firstOrder: boolean): CartPreview => {
  const discount = firstOrder && subtotal > 0 ? round(subtotal * (FIRST_ORDER_DISCOUNT_PERCENT / 100)) : 0;
  const deliveryFee = subtotal > 0 && subtotal < FREE_DELIVERY_THRESHOLD ? DELIVERY_FEE : 0;
  return { subtotal, discount, deliveryFee, total: round(subtotal - discount + deliveryFee) };
};

const round = (amount: number) => Math.round(amount * 100) / 100;
