import { describe, expect, it } from 'vitest';
import type { PricingRules } from '@/api/types';
import { previewTotals } from './pricing';

const rules: PricingRules = { freeDeliveryThreshold: 299, deliveryFee: 10, firstOrderDiscountPercent: 20 };

describe('cart preview totals', () => {
  it('charges delivery below the threshold and nothing at or above it', () => {
    expect(previewTotals(298.99, false, rules)).toEqual({ subtotal: 298.99, discount: 0, deliveryFee: 10, total: 308.99 });
    expect(previewTotals(299, false, rules)).toEqual({ subtotal: 299, discount: 0, deliveryFee: 0, total: 299 });
  });

  it('takes the first-order discount off the subtotal but decides delivery from the subtotal before it', () => {
    // 320 - 20 % = 256, which is below the threshold, yet delivery stays free: the threshold
    // is compared with the subtotal, the same way the order service does it
    expect(previewTotals(320, true, rules)).toEqual({ subtotal: 320, discount: 64, deliveryFee: 0, total: 256 });
  });

  it('shows nothing to pay for an empty cart', () => {
    expect(previewTotals(0, true, rules)).toEqual({ subtotal: 0, discount: 0, deliveryFee: 0, total: 0 });
  });

  it('follows whatever rules the store publishes', () => {
    const generous = { freeDeliveryThreshold: 50, deliveryFee: 5, firstOrderDiscountPercent: 50 };
    expect(previewTotals(40, true, generous)).toEqual({ subtotal: 40, discount: 20, deliveryFee: 5, total: 25 });
  });

  it('rounds to cents', () => {
    expect(previewTotals(10.01, true, rules)).toEqual({ subtotal: 10.01, discount: 2, deliveryFee: 10, total: 18.01 });
  });
});
