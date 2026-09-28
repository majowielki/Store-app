import type { ComponentType } from 'react';
import { RouterProvider, createBrowserRouter } from 'react-router-dom';
import { ErrorElement } from './components';
import { Error, HomeLayout, Landing } from './pages';
import { requireAdmin, requireUser } from './routes/guards';

/**
 * A route module loaded on first visit. Only the layout and the home page are in the main bundle;
 * every other page, the admin panel with its charts above all, comes when it is first opened.
 */
const page = (load: () => Promise<{ default: ComponentType }>) => async () => ({ Component: (await load()).default });

const router = createBrowserRouter([
  {
    path: '/',
    element: <HomeLayout />,
    errorElement: <Error />,
    children: [
      { index: true, element: <Landing />, errorElement: <ErrorElement /> },
      { path: 'products', lazy: page(() => import('./pages/Products')), errorElement: <ErrorElement /> },
      { path: 'products/:id', lazy: page(() => import('./pages/SingleProduct')), errorElement: <ErrorElement /> },
      { path: 'cart', lazy: page(() => import('./pages/Cart')), errorElement: <ErrorElement /> },
      { path: 'wishlist', lazy: page(() => import('./pages/Wishlist')), errorElement: <ErrorElement /> },
      { path: 'compare', lazy: page(() => import('./pages/Compare')), errorElement: <ErrorElement /> },
      { path: 'about', lazy: page(() => import('./pages/About')), errorElement: <ErrorElement /> },
      { path: 'contact', lazy: page(() => import('./pages/Contact')), errorElement: <ErrorElement /> },
      { path: 'checkout', lazy: page(() => import('./pages/Checkout')), errorElement: <ErrorElement />, loader: requireUser },
      { path: 'orders', lazy: page(() => import('./pages/Orders')), errorElement: <ErrorElement />, loader: requireUser },
      { path: 'orders/:id', lazy: page(() => import('./pages/OrderDetail')), errorElement: <ErrorElement />, loader: requireUser },
      { path: 'orders/:id/pay', lazy: page(() => import('./pages/PayOrder')), errorElement: <ErrorElement />, loader: requireUser },
      // Editorial pages (with the Markdown renderer)
      { path: 'makers', lazy: page(() => import('./pages/content/Makers')), errorElement: <ErrorElement /> },
      { path: 'makers/:slug', lazy: page(() => import('./pages/content/MakerPage')), errorElement: <ErrorElement /> },
      { path: 'collections', lazy: page(() => import('./pages/content/Collections')), errorElement: <ErrorElement /> },
      { path: 'collections/:slug', lazy: page(() => import('./pages/content/CollectionPage')), errorElement: <ErrorElement /> },
      { path: 'journal', lazy: page(() => import('./pages/content/Journal')), errorElement: <ErrorElement /> },
      { path: 'journal/:slug', lazy: page(() => import('./pages/content/ArticlePage')), errorElement: <ErrorElement /> },
      { path: 'looks', lazy: page(() => import('./pages/content/Looks')), errorElement: <ErrorElement /> },
      { path: 'looks/:slug', lazy: page(() => import('./pages/content/LookPage')), errorElement: <ErrorElement /> },
      // Help and legal pages: static text in the UI, they change with the code
      { path: 'help', lazy: page(() => import('./pages/info/Help')), errorElement: <ErrorElement /> },
      { path: 'shipping', lazy: page(() => import('./pages/info/Shipping')), errorElement: <ErrorElement /> },
      { path: 'returns', lazy: page(() => import('./pages/info/Returns')), errorElement: <ErrorElement /> },
      { path: 'terms', lazy: page(() => import('./pages/info/Terms')), errorElement: <ErrorElement /> },
      { path: 'privacy', lazy: page(() => import('./pages/info/Privacy')), errorElement: <ErrorElement /> },
    ],
  },
  {
    path: '/admin',
    lazy: page(() => import('./pages/admin/AdminLayout')),
    errorElement: <Error />,
    loader: requireAdmin,
    children: [
      { index: true, lazy: page(() => import('./pages/admin/Dashboard')), errorElement: <ErrorElement /> },
      { path: 'orders', lazy: page(() => import('./pages/admin/Orders')), errorElement: <ErrorElement /> },
      { path: 'orders/:id', lazy: page(() => import('./pages/admin/OrderDetail')), errorElement: <ErrorElement /> },
      { path: 'discount-codes', lazy: page(() => import('./pages/admin/DiscountCodes')), errorElement: <ErrorElement /> },
      { path: 'products', lazy: page(() => import('./pages/admin/Products')), errorElement: <ErrorElement /> },
      { path: 'products/new', lazy: page(() => import('./pages/admin/ProductForm')), errorElement: <ErrorElement /> },
      { path: 'products/:id', lazy: page(() => import('./pages/admin/ProductForm')), errorElement: <ErrorElement /> },
      { path: 'reviews', lazy: page(() => import('./pages/admin/Reviews')), errorElement: <ErrorElement /> },
      { path: 'users', lazy: page(() => import('./pages/admin/Users')), errorElement: <ErrorElement /> },
      { path: 'users/:id', lazy: page(() => import('./pages/admin/UserDetail')), errorElement: <ErrorElement /> },
      { path: 'users/:id/orders', lazy: page(() => import('./pages/admin/UserOrders')), errorElement: <ErrorElement /> },
      { path: 'content/:kind', lazy: page(() => import('./pages/admin/content/ContentList')), errorElement: <ErrorElement /> },
      { path: 'content/:kind/new', lazy: page(() => import('./pages/admin/content/ContentForm')), errorElement: <ErrorElement /> },
      { path: 'content/:kind/:id', lazy: page(() => import('./pages/admin/content/ContentForm')), errorElement: <ErrorElement /> },
    ],
  },
  { path: '/login', lazy: page(() => import('./pages/Login')), errorElement: <Error /> },
  { path: '/register', lazy: page(() => import('./pages/Register')), errorElement: <Error /> },
]);

const App = () => <RouterProvider router={router} />;

export default App;
