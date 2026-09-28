import type {
  ApiCartResponse,
  Article,
  AuthResponse,
  Collection,
  Lookbook,
  Maker,
  Order,
  PricingRules,
  Product,
  ProductDetail,
  ProblemDetails,
  ProductsMeta,
  Review,
  ReviewSummary,
  AdminReview,
  TestCard,
  UserResponse,
} from '@/api/types';

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
  slug: 'oak-table',
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
  availability: 'inStock',
  availableQuantity: 12,
  ratingAverage: 4.5,
  ratingCount: 4,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
  ...overrides,
});

/** The same table as its page loads it: two more pictures and a point on the chair in the room. */
export const productDetail = (overrides: Partial<ProductDetail> = {}): ProductDetail => ({
  ...product(),
  images: [
    { url: 'https://images.example.com/oak-table-2.jpg', alt: 'The oak grain up close' },
    { url: 'https://images.example.com/oak-table-3.jpg', alt: 'The Oak Table on its own' },
  ],
  hotspots: [
    { x: 30, y: 60, productSlug: 'pine-chair' },
    { x: 80, y: 20, productSlug: 'retired-lamp' },
  ],
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
  counts: {
    total: 3,
    categories: { tables: 2, chairs: 1 },
    groups: { furniture: 3 },
    companies: { luxora: 3 },
    colors: { brown: 2, black: 1 },
    sale: 1,
    newArrival: 0,
  },
};

/** The store's pricing as the order service publishes it. */
export const pricingRules: PricingRules = {
  freeDeliveryThreshold: 299,
  deliveryFee: 10,
  firstOrderDiscountPercent: 20,
  deliveryFrom: '2026-09-30',
  deliveryTo: '2026-10-02',
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
  statusHistory: [{ status: 'Placed', changedAt: '2026-01-02T00:00:00Z' }],
  nextStatuses: ['Cancelled'],
  deliveryFrom: '2026-01-06',
  deliveryTo: '2026-01-08',
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

// Content as the content service publishes it; products are named by slug
const published = { isPublished: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z' };

export const maker = (overrides: Partial<Maker> = {}): Maker => ({
  id: 1,
  slug: 'luxora',
  name: 'Luxora',
  company: 'luxora',
  tagline: 'Oak, joined by hand.',
  story: '## From the workshop\n\nEvery table starts as a plank.',
  location: 'Kraków',
  foundedYear: 1998,
  coverImage: 'https://images.example.com/maker.webp',
  ...published,
  ...overrides,
});

export const collection = (overrides: Partial<Collection> = {}): Collection => ({
  id: 1,
  slug: 'warm-minimal',
  title: 'Warm Minimal',
  summary: 'Fewer things, all of them oak.',
  body: '## Why oak\n\nIt ages well.',
  coverImage: 'https://images.example.com/collection.webp',
  productSlugs: ['pine-chair', 'oak-table'],
  sortOrder: 1,
  ...published,
  ...overrides,
});

export const article = (overrides: Partial<Article> = {}): Article => ({
  id: 1,
  slug: 'caring-for-oak',
  title: 'Caring for oak',
  excerpt: 'Oil it twice a year.',
  body: '## Oil, not polish\n\nA thin coat is enough.',
  coverImage: 'https://images.example.com/article.webp',
  author: 'Ewa',
  publishedAt: '2026-09-01T12:00:00Z',
  productSlugs: ['oak-table'],
  ...published,
  ...overrides,
});

export const lookbook = (overrides: Partial<Lookbook> = {}): Lookbook => ({
  id: 1,
  slug: 'dining-room',
  title: 'A dining room',
  summary: 'A table for six and a chair to match.',
  image: 'https://images.example.com/look.webp',
  hotspots: [
    { x: 30, y: 60, productSlug: 'oak-table' },
    { x: 70, y: 40, productSlug: 'pine-chair' },
  ],
  sortOrder: 1,
  ...published,
  ...overrides,
});

export const review = (overrides: Partial<Review> = {}): Review => ({
  id: 'b7a1c2d3-0000-4000-8000-000000000001',
  productId: 7,
  authorName: 'Marta S.',
  rating: 5,
  title: 'Beautiful and solid',
  body: 'The oak has a lovely grain and nothing wobbles when you lean on it.',
  verifiedPurchase: true,
  status: 'published',
  reported: false,
  createdAt: '2026-08-01T10:00:00Z',
  ...overrides,
});

export const reviewSummary = (overrides: Partial<ReviewSummary> = {}): ReviewSummary => ({
  productId: 7,
  averageRating: 4.5,
  reviewCount: 2,
  distribution: { '1': 0, '2': 0, '3': 0, '4': 1, '5': 1 },
  ...overrides,
});

export const adminReview = (overrides: Partial<AdminReview> = {}): AdminReview => ({
  id: 'c8b2d3e4-0000-4000-8000-000000000002',
  productId: 7,
  productSlug: null,
  userId: 'user-1',
  authorName: 'Anna N.',
  rating: 4,
  title: 'Nearly perfect',
  body: 'Lovely table, one leg needed a shim on our old floor.',
  status: 'pending',
  reported: false,
  reportCount: 0,
  reportReasons: [],
  rejectionReason: null,
  source: 'customer',
  verifiedPurchase: true,
  demo: false,
  expiresAt: null,
  createdAt: '2026-09-27T10:00:00Z',
  submittedAt: '2026-09-27T10:00:00Z',
  moderatedAt: null,
  moderatedBy: null,
  ...overrides,
});

/** The cards the payment service takes in test mode, as it lists them. */
export const testCards: TestCard[] = [
  { number: '4242 4242 4242 4242', outcome: 'approved' },
  { number: '4000 0000 0000 3220', outcome: 'authenticationRequired' },
  { number: '4000 0000 0000 9995', outcome: 'insufficientFunds' },
  { number: '4000 0000 0000 0002', outcome: 'declined' },
];
