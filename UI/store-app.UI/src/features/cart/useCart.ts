import { useCallback, useMemo } from 'react';
import { useAddCartItemMutation, useGetCartQuery, useRemoveCartItemMutation, useUpdateCartItemMutation } from '@/api/cart';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { itemAdded, itemRemoved, quantityChanged, type GuestCartItem } from './guestCartSlice';

/** One line of the cart as the pages show it, whichever cart it comes from. */
export interface CartLine {
  /** Stable key for lists: the server line id, or product and colour for a guest line. */
  key: string;
  /** Present for a line of the server cart. */
  serverItemId?: number;
  productId: number;
  title: string;
  image: string;
  company: string;
  color: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface CartView {
  lines: CartLine[];
  totalItems: number;
  subtotal: number;
  /** The server cart is being read for the first time. */
  isLoading: boolean;
}

const round = (amount: number) => Math.round(amount * 100) / 100;

/**
 * The cart: the server's for a signed-in user (the source of truth, priced by the catalogue),
 * this browser's for a visitor. Components read one shape either way.
 */
export const useCart = (): CartView => {
  const user = useAppSelector((state) => state.session.user);
  const guestItems = useAppSelector((state) => state.guestCart.items);
  const { data: serverCart, isLoading } = useGetCartQuery(undefined, { skip: !user });

  return useMemo(() => {
    const lines: CartLine[] = user
      ? (serverCart?.items ?? []).map((item) => ({
          key: `server-${item.id}`,
          serverItemId: item.id,
          productId: item.productId,
          title: item.title,
          image: item.image,
          company: item.company,
          color: item.color,
          unitPrice: item.price,
          quantity: item.quantity,
          lineTotal: item.lineTotal,
        }))
      : guestItems.map((item) => ({
          key: `guest-${item.productId}-${item.color}`,
          productId: item.productId,
          title: item.title,
          image: item.image,
          company: item.company,
          color: item.color,
          unitPrice: item.unitPrice,
          quantity: item.quantity,
          lineTotal: round(item.unitPrice * item.quantity),
        }));
    return {
      lines,
      totalItems: lines.reduce((sum, line) => sum + line.quantity, 0),
      subtotal: user ? (serverCart?.total ?? 0) : round(lines.reduce((sum, line) => sum + line.lineTotal, 0)),
      isLoading: !!user && isLoading,
    };
  }, [user, serverCart, guestItems, isLoading]);
};

export interface CartActions {
  /** Adds a line; a rejected server request throws (the toast has been shown already). */
  add: (item: GuestCartItem) => Promise<void>;
  setQuantity: (line: CartLine, quantity: number) => Promise<void>;
  remove: (line: CartLine) => Promise<void>;
}

/** Changes go to the server cart for a signed-in user and to the browser cart for a visitor. */
export const useCartActions = (): CartActions => {
  const user = useAppSelector((state) => state.session.user);
  const dispatch = useAppDispatch();
  const [addItem] = useAddCartItemMutation();
  const [updateItem] = useUpdateCartItemMutation();
  const [removeItem] = useRemoveCartItemMutation();

  const add = useCallback(
    async (item: GuestCartItem) => {
      if (user) {
        await addItem({ productId: item.productId, quantity: item.quantity, color: item.color }).unwrap();
      } else {
        dispatch(itemAdded(item));
      }
    },
    [user, addItem, dispatch],
  );

  const setQuantity = useCallback(
    async (line: CartLine, quantity: number) => {
      if (user && line.serverItemId !== undefined) {
        await updateItem({ itemId: line.serverItemId, quantity }).unwrap();
      } else {
        dispatch(quantityChanged({ productId: line.productId, color: line.color, quantity }));
      }
    },
    [user, updateItem, dispatch],
  );

  const remove = useCallback(
    async (line: CartLine) => {
      if (user && line.serverItemId !== undefined) {
        await removeItem(line.serverItemId).unwrap();
      } else {
        dispatch(itemRemoved({ productId: line.productId, color: line.color }));
      }
    },
    [user, removeItem, dispatch],
  );

  return { add, setQuantity, remove };
};
