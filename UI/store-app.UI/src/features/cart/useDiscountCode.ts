import { useCheckDiscountCodeQuery } from '@/api/orders';
import { errorMessage } from '@/api/problem';
import type { DiscountCodeCheck } from '@/api/types';
import { useAppSelector } from '@/hooks';
import { useCart } from './useCart';

export interface DiscountCodeView {
  /** The code typed in the cart, if any. */
  code: string | null;
  /** What the code takes off the cart's subtotal now; missing while it is checked or when it is refused. */
  check?: DiscountCodeCheck;
  /** Why the code cannot be used, in the order service's words. */
  refusal?: string;
  isChecking: boolean;
}

/** The typed code, checked against the cart's current subtotal (again whenever it changes). */
export const useDiscountCode = (): DiscountCodeView => {
  const code = useAppSelector((state) => state.discountCode.code);
  const { subtotal } = useCart();
  const { currentData, error, isFetching } = useCheckDiscountCodeQuery({ code: code ?? '', subtotal }, { skip: !code || subtotal <= 0 });
  return {
    code,
    check: code ? currentData : undefined,
    refusal: code && error && !isFetching ? errorMessage(error) : undefined,
    isChecking: isFetching,
  };
};
