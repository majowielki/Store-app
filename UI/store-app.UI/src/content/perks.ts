import { Percent, RotateCcw, ShieldCheck, Truck, type LucideIcon } from 'lucide-react';
import { useGetPricingRulesQuery } from '@/api/orders';
import { formatAsDollars } from '@/utils';

export interface Perk {
  key: string;
  icon: LucideIcon;
  title: string;
  description: string;
  /** The perk in one line, for the announcement bar. */
  announcement: string;
}

/** Whole dollars without the cents, the way banners quote amounts. */
const wholeDollars = (amount: number) => formatAsDollars(amount).replace(/\.00$/, '');

/**
 * What the shop promises every customer. The delivery threshold and the first-order discount
 * are the order service's own numbers (GET /orders/pricing-rules); until they arrive, or when
 * they cannot, the copy stays general rather than quoting a number that may be wrong.
 */
export const usePerks = (): Perk[] => {
  const { data: rules } = useGetPricingRulesQuery();
  const threshold = rules ? wholeDollars(rules.freeDeliveryThreshold) : null;
  const discount = rules ? `${rules.firstOrderDiscountPercent}%` : null;
  return [
    {
      key: 'delivery',
      icon: Truck,
      title: 'Free delivery',
      description: threshold ? `On every order over ${threshold}` : 'On bigger orders',
      announcement: threshold ? `Free delivery on orders over ${threshold}` : 'Free delivery on bigger orders',
    },
    {
      key: 'welcome',
      icon: Percent,
      title: discount ? `${discount} off your first order` : 'A welcome discount',
      description: 'Taken off at checkout, no code needed',
      announcement: discount ? `${discount} off your first order — no code needed` : 'A welcome discount on your first order',
    },
    {
      key: 'returns',
      icon: RotateCcw,
      title: '30-day returns',
      description: 'Changed your mind? Send it back',
      announcement: '30-day returns, no questions asked',
    },
    {
      key: 'warranty',
      icon: ShieldCheck,
      title: '2-year warranty',
      description: 'On every piece we sell',
      announcement: '2-year warranty on every piece',
    },
  ];
};
