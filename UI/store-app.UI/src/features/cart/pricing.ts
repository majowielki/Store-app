import type { PricingRules } from '@/api/types';

// The cart page previews the order's amounts with the rules the order service publishes
// (GET /orders/pricing-rules); the order itself is always priced by the server. No number
// of the store's pricing is written here.

export interface CartPreview {
  subtotal: number;
  discount: number;
  deliveryFee: number;
  total: number;
}

/** The amounts the cart page shows; the delivery threshold is compared with the subtotal before the discount. */
export const previewTotals = (subtotal: number, firstOrder: boolean, rules: PricingRules): CartPreview => {
  const discount = firstOrder && subtotal > 0 ? round(subtotal * (rules.firstOrderDiscountPercent / 100)) : 0;
  const deliveryFee = subtotal > 0 && subtotal < rules.freeDeliveryThreshold ? rules.deliveryFee : 0;
  return { subtotal, discount, deliveryFee, total: round(subtotal - discount + deliveryFee) };
};

const round = (amount: number) => Math.round(amount * 100) / 100;
