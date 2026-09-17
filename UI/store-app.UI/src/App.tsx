import type { ComponentType } from 'react';
import { RouterProvider, createBrowserRouter } from 'react-router-dom';
import { ErrorElement } from './components';
import {
  About,
  Cart,
  Checkout,
  Contact,
  Error,
  HomeLayout,
  Landing,
  Login,
  OrderDetail,
  Orders,
  Products,
  Register,
  SingleProduct,
} from './pages';
import { requireAdmin, requireUser } from './routes/guards';

/** A route module loaded on first visit; the admin panel (with its charts) stays out of the shop's bundle this way. */
const page = (load: () => Promise<{ default: ComponentType }>) => async () => ({ Component: (await load()).default });

const router = createBrowserRouter([
  {
    path: '/',
    element: <HomeLayout />,
    errorElement: <Error />,
    children: [
      { index: true, element: <Landing />, errorElement: <ErrorElement /> },
      { path: 'products', element: <Products />, errorElement: <ErrorElement /> },
      { path: 'products/:id', element: <SingleProduct />, errorElement: <ErrorElement /> },
      { path: 'cart', element: <Cart />, errorElement: <ErrorElement /> },
      { path: 'about', element: <About />, errorElement: <ErrorElement /> },
      { path: 'contact', element: <Contact />, errorElement: <ErrorElement /> },
      { path: 'checkout', element: <Checkout />, errorElement: <ErrorElement />, loader: requireUser },
      { path: 'orders', element: <Orders />, errorElement: <ErrorElement />, loader: requireUser },
      { path: 'orders/:id', element: <OrderDetail />, errorElement: <ErrorElement />, loader: requireUser },
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
      { path: 'products', lazy: page(() => import('./pages/admin/Products')), errorElement: <ErrorElement /> },
      { path: 'products/new', lazy: page(() => import('./pages/admin/ProductForm')), errorElement: <ErrorElement /> },
      { path: 'products/:id', lazy: page(() => import('./pages/admin/ProductForm')), errorElement: <ErrorElement /> },
      { path: 'users', lazy: page(() => import('./pages/admin/Users')), errorElement: <ErrorElement /> },
      { path: 'users/:id', lazy: page(() => import('./pages/admin/UserDetail')), errorElement: <ErrorElement /> },
      { path: 'users/:id/orders', lazy: page(() => import('./pages/admin/UserOrders')), errorElement: <ErrorElement /> },
    ],
  },
  { path: '/login', element: <Login />, errorElement: <Error /> },
  { path: '/register', element: <Register />, errorElement: <Error /> },
]);

const App = () => <RouterProvider router={router} />;

export default App;
