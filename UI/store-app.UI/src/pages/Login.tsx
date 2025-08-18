import { useAppDispatch } from '@/hooks';
/* eslint-disable react-refresh/only-export-components */
import { Form, Link, redirect, type ActionFunction } from 'react-router-dom';
import { Card, CardHeader, CardContent, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { SubmitBtn, FormInput } from '@/components';
import { useState } from 'react';
import { validateLogin } from '@/utils/validation';
import { toast } from '@/hooks/use-toast';
import { type ReduxStore } from '@/store';
import { loginUserAsync, getCurrentUserAsync } from '@/features/user/userSlice';
import { authApi } from '@/utils/api';
import { mergeLocalCartToServer, fetchCart } from '@/features/cart/cartSlice';

export const action =
  (store: ReduxStore): ActionFunction =>
  async ({ request }): Promise<Response | null> => {
    const formData = await request.formData();
      const credentials = {
        email: formData.get('email')?.toString() || '',
        password: formData.get('password')?.toString() || '',
      };
    try {
      // Use async thunk for real login
      const result = await store.dispatch(loginUserAsync(credentials));
      if (loginUserAsync.fulfilled.match(result)) {
        // Optionally fetch real user profile using /auth/me if enabled
        if (import.meta.env.VITE_USE_AUTH_ME === 'true') {
          await store.dispatch(getCurrentUserAsync());
        }
        // sync guest cart to server, then fetch server cart
        await store.dispatch(mergeLocalCartToServer());
        await store.dispatch(fetchCart());
        const roles: string[] = result.payload?.user?.roles || [];
        return redirect(roles.includes('Admin') || roles.includes('admin') ? '/admin' : '/');
      } else {
        toast({ description: result.payload as string || 'Login failed' });
        return null;
      }
    } catch {
      toast({ description: 'Login failed' });
      return null;
    }
  };

const Login = () => {
  const dispatch = useAppDispatch();
  const [form, setForm] = useState({ email: '', password: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const loginAsGuestUser = async (): Promise<void> => {
    try {
      const response = await authApi.demoLogin();
      if (response.success && response.accessToken && response.user) {
        localStorage.setItem('authToken', response.accessToken);
        localStorage.setItem('authUser', JSON.stringify(response.user));
        await dispatch({ type: 'user/setUser', payload: response.user });
        await dispatch({ type: 'user/setToken', payload: response.accessToken });
        toast({ description: 'Demo user logged in!' });
        window.location.href = '/';
      } else {
        toast({ description: response.message || 'Demo user login failed', variant: 'destructive' });
      }
    } catch {
      toast({ description: 'Demo user login failed', variant: 'destructive' });
    }
  };

  const loginAsDemoAdmin = async (): Promise<void> => {
    try {
      const response = await authApi.demoAdminLogin();
      if (response.success && response.accessToken && response.user) {
        localStorage.setItem('authToken', response.accessToken);
        localStorage.setItem('authUser', JSON.stringify(response.user));
        await dispatch({ type: 'user/setUser', payload: response.user });
        await dispatch({ type: 'user/setToken', payload: response.accessToken });
        toast({ description: 'Demo admin logged in!' });
        window.location.href = '/admin';
      } else {
        toast({ description: response.message || 'Demo admin login failed', variant: 'destructive' });
      }
    } catch {
      toast({ description: 'Demo admin login failed', variant: 'destructive' });
    }
  };

  const handleClose = () => {
    // Check if previous page was login or register
    const referrer = document.referrer;
    if (referrer && (referrer.includes('/login') || referrer.includes('/register'))) {
      window.location.href = '/';
      return;
    }
    if (window.history.length > 1) {
      window.history.back();
    } else {
      window.location.href = '/';
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const handleValidate = () => {
    const validation = validateLogin(form);
    setErrors(validation);
    return Object.keys(validation).length === 0;
  };

  return (
    <section className="h-screen grid place-items-center">
      <Card className="w-96 bg-muted relative">
          <button
            onClick={handleClose}
            className="absolute top-2 right-2 text-xl px-2 py-1 rounded hover:bg-gray-200"
            title="Close"
          >
            ×
          </button>
        <CardHeader>
          <CardTitle className="text-center">Login</CardTitle>
        </CardHeader>
        <CardContent>
          <Form method="post" className="space-y-4" onSubmit={e => { if (!handleValidate()) { e.preventDefault(); } }}>
            <FormInput type="email" name="email" value={form.email} onChange={handleChange} />
            {errors.email && <div className='text-red-500 text-xs mb-1'>{errors.email}</div>}
            <FormInput type="password" name="password" value={form.password} onChange={handleChange} />
            {errors.password && <div className='text-red-500 text-xs mb-1'>{errors.password}</div>}
            <SubmitBtn text="Login" className="w-full mt-4" />
            <div className='flex gap-2 mt-4'>
              <Button
                type='button'
                variant='outline'
                className='w-1/2'
                onClick={loginAsGuestUser}
              >
                Demo User
              </Button>
              <Button
                type='button'
                variant='outline'
                className='w-1/2'
                onClick={loginAsDemoAdmin}
              >
                Demo Admin
              </Button>
            </div>
            <p className="text-center mt-4">
              Not a member?{' '}
              <Button type="button" asChild variant="link">
                <Link to="/register">Register</Link>
              </Button>
            </p>
          </Form>
        </CardContent>
      </Card>
    </section>
  );
}
export default Login;
