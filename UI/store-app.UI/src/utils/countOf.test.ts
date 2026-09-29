import { describe, expect, it } from 'vitest';
import { countOf } from './countOf';

describe('countOf', () => {
  it('puts the noun in the singular for one and in the plural otherwise', () => {
    expect(countOf(1, 'item')).toBe('1 item');
    expect(countOf(0, 'item')).toBe('0 items');
    expect(countOf(3, 'piece')).toBe('3 pieces');
    expect(countOf(2, 'person', 'people')).toBe('2 people');
  });
});
