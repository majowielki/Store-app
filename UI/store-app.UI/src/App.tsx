
import { RouterProvider, createBrowserRouter, redirect } from "react-router-dom";

import {
  HomeLayout,
  Landing,
  Error,
  Products,
  SingleProduct,
  Cart,
  About,
  Contact,
  Register,
  Login,
  Checkout,
  Orders,
  // user order detail
  OrderDetail as UserOrderDetail,
} from "./pages";
import { ErrorElement } from "./components";

import { loader as landingLoader } from "./pages/Landing";
import { loader as productsLoader } from "./pages/Products";
import { loader as singleProductLoader } from "./pages/SingleProduct";
import { loader as checkoutLoader } from './pages/Checkout';
import { loader as ordersLoader } from './pages/Orders';

import { action as registerUser } from './pages/Register';
import { action as loginUser } from './pages/Login';
import { action as checkoutAction } from './components/CheckoutForm';

import { store } from './store';
import AdminLayout from './pages/admin/AdminLayout';
import Dashboard from './pages/admin/Dashboard';
import AdminOrders from './pages/admin/Orders';
import AdminOrderDetail from './pages/admin/OrderDetail';
import AdminProducts from './pages/admin/Products';
import AdminProductForm from './pages/admin/ProductForm';
import AdminUsers from './pages/admin/Users';
import AdminUserDetail from './pages/admin/UserDetail';
import AdminUserOrders from './pages/admin/UserOrders';
const router = createBrowserRouter([
  {
    path: "/",
    element: <HomeLayout />,
    errorElement: <Error />,
    children: [
      {
        index: true,
        element: <Landing />,
        errorElement: <ErrorElement />,
        loader: landingLoader,
      },
      {
        path: "products",
        element: <Products />,
        errorElement: <ErrorElement />,
        loader: productsLoader,
      },
      {
        path: "products/:id",
        element: <SingleProduct />,
        errorElement: <ErrorElement />,
        loader: singleProductLoader,
      },
      {
        path: "cart",
        element: <Cart />,
        errorElement: <ErrorElement />,
      },
      { path: "about", element: <About />, errorElement: <ErrorElement /> },
  { path: "kontakt", element: <Contact />, errorElement: <ErrorElement /> },
  { path: "contact", element: <Contact />, errorElement: <ErrorElement /> },
      {
        path: "checkout",
        element: <Checkout />,
        errorElement: <ErrorElement />,
        loader: checkoutLoader(store),
        action: checkoutAction(store),
      },
      {
        path: "orders",
        element: <Orders />,
        errorElement: <ErrorElement />,
        loader: ordersLoader(store),
      },
      {
        path: "orders/:id",
        element: <UserOrderDetail />,
        errorElement: <ErrorElement />,
      },
    ],
  },
  {
    path: '/admin',
    element: <AdminLayout />,
    errorElement: <Error />,
    loader: async () => {
      const state = store.getState();
  const roles = state.userState.user?.roles || [];
  const isAdmin = roles.some((r) => /admin/i.test(r));
  if (!isAdmin) {
        return redirect('/');
      }
      return null;
    },
    children: [
  { index: true, element: <Dashboard /> },
  { path: 'orders', element: <AdminOrders /> },
  { path: 'orders/:id', element: <AdminOrderDetail /> },
  { path: 'products', element: <AdminProducts /> },
  { path: 'products/:id', element: <AdminProductForm /> },
  { path: 'products/new', element: <AdminProductForm /> },
  { path: 'users', element: <AdminUsers /> },
  { path: 'users/:id', element: <AdminUserDetail /> },
  { path: 'users/:id/orders', element: <AdminUserOrders /> },
    ],
  },
  {
    path: "/login",
    element: <Login />,
    errorElement: <Error />,
    action: loginUser(store),
  },
  {
    path: "/register",
    element: <Register />,
    errorElement: <Error />,
    action: registerUser,
  },
]);

const App = () => <RouterProvider router={router} />;
export default App;
