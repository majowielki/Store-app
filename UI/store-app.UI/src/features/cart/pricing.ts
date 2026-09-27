import type { PricingRules } from '@/api/types';

// The cart page previews the order's amounts with the rules the order service publishes
// (GET /orders/pricing-rules); the order itself is always priced by the server. No number
// of the store's pricing is written here.

/** The part of the rules that sets the amounts; the delivery window does not. */
export type PricingAmounts = Pick<PricingRules, 'freeDeliveryThreshold' | 'deliveryFee' | 'firstOrderDiscountPercent'>;

export interface CartPreview {
  subtotal: number;
  discount: number;
  /** Which discount is taken: the first-order one or the code's; none without a discount. */
  discountReason: 'first-order' | 'code' | null;
  deliveryFee: number;
  total: number;
}

/**
 * The amounts the cart page shows; the delivery threshold is compared with the subtotal before
 * the discount. The first-order discount and a code's (codeDiscount, as the order
 * service computed it) do not add up: the larger is taken, the first-order one on a tie.
 */
export const previewTotals = (subtotal: number, firstOrder: boolean, rules: PricingAmounts, codeDiscount = 0): CartPreview => {
  const firstOrderDiscount = firstOrder && subtotal > 0 ? percentOf(subtotal, rules.firstOrderDiscountPercent) : 0;
  const [discount, discountReason] =
    codeDiscount > firstOrderDiscount
      ? ([codeDiscount, 'code'] as const)
      : ([firstOrderDiscount, firstOrderDiscount > 0 ? ('first-order' as const) : null] as const);
  const deliveryFee = subtotal > 0 && subtotal < rules.freeDeliveryThreshold ? rules.deliveryFee : 0;
  return { subtotal, discount, discountReason, deliveryFee, total: round(subtotal - discount + deliveryFee) };
};

const round = (amount: number) => Math.round(amount * 100) / 100;

/**
 * A percentage of an amount, to the cent, half a cent going up as on the server. Counted in
 * whole cents: 0.58 * 25 % is 0.145 on paper but 0.14499... in binary fractions.
 */
const percentOf = (amount: number, percent: number) => Math.round((Math.round(amount * 100) * percent) / 100) / 100;
