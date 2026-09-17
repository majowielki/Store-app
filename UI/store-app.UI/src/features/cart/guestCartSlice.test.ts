import { describe, expect, it } from 'vitest';
import reducer, { cleared, itemAdded, itemRemoved, loadGuestCart, quantityChanged, saveGuestCart, type GuestCartItem } from './guestCartSlice';

const table: GuestCartItem = { productId: 7, title: 'Oak Table', image: 'img', company: 'luxora', color: 'brown', unitPrice: 320, quantity: 1 };

describe('guest cart', () => {
  it('adds a line and raises the quantity of the same product and colour', () => {
    let state = reducer(undefined, itemAdded(table));
    state = reducer(state, itemAdded({ ...table, quantity: 2, unitPrice: 300 }));
    state = reducer(state, itemAdded({ ...table, color: 'black' }));

    expect(state.items).toEqual([
      { ...table, quantity: 3, unitPrice: 300 },
      { ...table, color: 'black' },
    ]);
  });

  it('changes a quantity (never below one) and removes a line', () => {
    let state = reducer({ items: [table] }, quantityChanged({ productId: 7, color: 'brown', quantity: 0 }));
    expect(state.items[0].quantity).toBe(1);

    state = reducer(state, quantityChanged({ productId: 7, color: 'brown', quantity: 4 }));
    expect(state.items[0].quantity).toBe(4);

    state = reducer(state, itemRemoved({ productId: 7, color: 'brown' }));
    expect(state.items).toEqual([]);
  });

  it('survives a page load through localStorage and ignores anything unreadable', () => {
    saveGuestCart({ items: [table] });
    expect(loadGuestCart()).toEqual({ items: [table] });

    localStorage.setItem('guestCart', '{"items":[{"productId":"x"},{"productId":1,"color":"red","quantity":1,"unitPrice":2}]}');
    expect(loadGuestCart().items).toEqual([{ productId: 1, color: 'red', quantity: 1, unitPrice: 2 }]);

    localStorage.setItem('guestCart', 'not json');
    expect(loadGuestCart()).toEqual({ items: [] });
  });

  it('is emptied after the merge into the server cart', () => {
    expect(reducer({ items: [table] }, cleared())).toEqual({ items: [] });
  });
});
