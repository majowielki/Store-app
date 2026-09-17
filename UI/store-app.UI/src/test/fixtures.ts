import type { ApiCartResponse, AuthResponse, Order, Product, ProblemDetails, ProductsMeta, UserResponse } from '@/api/types';

// Fixtures are typed with the generated contract, so a change of a response shape on the
// server (after "npm run api:generate") fails here before it fails on a page.

export const user: UserResponse = {
  id: 'user-1',
  email: 'anna@example.com',
  userName: 'anna@example.com',
  firstName: 'Anna',
  lastName: 'Nowak',
  displayName: 'Anna Nowak',
  simpleAddress: 'Main Street 1',
  roles: ['user'],
  isActive: true,
  isDemo: false,
  createdAt: '2026-01-01T00:00:00Z',
};

export const admin: UserResponse = {
  ...user,
  id: 'admin-1',
  email: 'admin@example.com',
  userName: 'admin@example.com',
  displayName: 'Store Admin',
  roles: ['true-admin'],
};

export const session = (who: UserResponse = user): AuthResponse => ({
  accessToken: `token-for-${who.id}`,
  expiresAt: '2099-01-01T00:00:00Z',
  user: who,
});

export const product = (overrides: Partial<Product> = {}): Product => ({
  id: 7,
  title: 'Oak Table',
  description: 'A sturdy oak table for six.',
  price: 400,
  salePrice: 320,
  effectivePrice: 320,
  category: 'tables',
  company: 'luxora',
  newArrival: false,
  image: 'https://images.example.com/oak-table.jpg',
  colors: ['brown', 'black'],
  groups: ['furniture'],
  materials: ['oak'],
  widthCm: null,
  heightCm: null,
  depthCm: null,
  weightKg: null,
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
  ...overrides,
});

export const emptyCart: ApiCartResponse = {
  id: 1,
  userId: user.id,
  items: [],
  totalItems: 0,
  total: 0,
  updatedAt: '2026-01-01T00:00:00Z',
  isEmpty: true,
  priceChanged: false,
};

/** A server cart with one line of the oak table (sale price), as the cart service prices it. */
export const cartWithTable = (quantity = 1): ApiCartResponse => ({
  ...emptyCart,
  items: [
    {
      id: 11,
      productId: 7,
      title: 'Oak Table',
      image: 'https://images.example.com/oak-table.jpg',
      price: 320,
      quantity,
      color: 'brown',
      company: 'luxora',
      lineTotal: 320 * quantity,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:00:00Z',
    },
  ],
  totalItems: quantity,
  total: 320 * quantity,
  isEmpty: false,
});

export const meta: ProductsMeta = {
  categories: ['tables', 'chairs'],
  groups: ['furniture'],
  companies: ['luxora'],
  colors: ['brown', 'black'],
  groupCategoryMap: [{ key: 'furniture', name: 'Furniture', categories: [{ key: 'tables', name: 'Tables' }] }],
};

export const order = (overrides: Partial<Order> = {}): Order => ({
  id: 100,
  userId: user.id,
  userEmail: user.email,
  customerName: 'Anna Nowak',
  deliveryAddress: 'Main Street 1',
  subtotal: 320,
  discountAmount: 0,
  discountReason: null,
  deliveryFee: 0,
  total: 320,
  totalItems: 1,
  status: 'Placed',
  createdAt: '2026-01-02T00:00:00Z',
  orderItems: [
    {
      id: 1,
      productId: 7,
      productTitle: 'Oak Table',
      productImage: 'https://images.example.com/oak-table.jpg',
      color: 'brown',
      company: 'luxora',
      quantity: 1,
      price: 320,
      lineTotal: 320,
    },
  ],
  ...overrides,
});

export const problem = (status: number, detail: string, errors?: Record<string, string[]>): ProblemDetails => ({
  type: 'about:blank',
  title: 'Error',
  status,
  detail,
  instance: '/api/v1/test',
  traceId: '00-test-00',
  errors,
});
