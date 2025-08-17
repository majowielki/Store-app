export type Category = {
  slug: string;
  label: string;
  // group key used in /products?group=... or for special tabs like 'sale'/'new'
  group: string;
};

// Top-level navigation categories; 'All products' is rendered separately in the nav
export const categories: Category[] = [
  { slug: 'furniture', label: 'Furniture', group: 'furniture' },
  { slug: 'kitchen', label: 'Kitchen', group: 'kitchen' },
  { slug: 'bathroom', label: 'Bathroom', group: 'bathroom' },
  { slug: 'decorations', label: 'Decorations', group: 'decorations' },
  { slug: 'lamps', label: 'Lamps', group: 'lamps' },
  { slug: 'kids', label: 'Kids', group: 'kids' },
  { slug: 'garden', label: 'Garden', group: 'garden' },
  { slug: 'new-arrivals', label: 'New Arrivals', group: 'newarrivals' },
  { slug: 'sale', label: 'Sale', group: 'sale' },
];
