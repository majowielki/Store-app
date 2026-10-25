import { describe, expect, it } from 'vitest';
import { materialsIn } from './materials';

describe('the materials a product is made of', () => {
  it('finds the guide entries a materials list names, whatever the case and accents', () => {
    expect(materialsIn(['Linen-Weave Fabric', 'Pine', 'Foam', 'Solid Oak']).map((m) => m.key)).toEqual(['oak', 'linen']);
    expect(materialsIn(['Bouclé upholstery', 'Walnut veneer']).map((m) => m.key)).toEqual(['walnut', 'boucle']);
    expect(materialsIn(['Steel'])).toEqual([]);
  });
});
