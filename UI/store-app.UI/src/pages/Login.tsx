/* eslint-disable react-refresh/only-export-components */
import { Form, Link, redirect, type ActionFunction, useNavigate } from 'react-router-dom';
import { Card, CardHeader, CardContent, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { SubmitBtn, FormInput } from '@/components';
import { customFetch } from '@/utils';
import { toast } from '@/hooks/use-toast';
import { type ReduxStore } from '@/store';
import { loginUser, getCurrentUserAsync } from '@/features/user/userSlice';
import { mergeLocalCartToServer, fetchCart } from '@/features/cart/cartSlice';
import { useAppDispatch } from '@/hooks';
import { AxiosError, AxiosResponse } from 'axios';

export const action =
  (store: ReduxStore): ActionFunction =>
  async ({ request }): Promise<Response | null> => {
    const formData = await request.formData();
    const data = Object.fromEntries(formData);
    try {
      const response: AxiosResponse = await customFetch.post('/auth/login', data);
      // API returns { success, message, accessToken, expiresAt, user }
      const jwt: string = response.data.accessToken;
      const user = response.data.user;
      const username: string = user?.displayName || user?.userName || user?.email || 'User';
  store.dispatch(loginUser({ username, jwt }));
  // Fetch real user profile using the stored token
  // Fetch user profile to confirm token works
  await store.dispatch(getCurrentUserAsync());
  // sync guest cart to server, then fetch server cart
  await store.dispatch(mergeLocalCartToServer());
  await store.dispatch(fetchCart());
      return redirect('/');
    } catch (error) {
      let errorMsg = 'Login failed';
      if (error instanceof AxiosError) {
        const respData: unknown = error.response?.data;
        if (typeof respData === 'string') errorMsg = respData;
        else if (respData && typeof respData === 'object') {
          const obj = respData as { message?: string; errors?: Record<string, string[]> };
          if (obj.message) errorMsg = obj.message;
          else if (obj.errors) {
            const firstKey = Object.keys(obj.errors)[0];
            const firstErr = obj.errors[firstKey]?.[0];
            if (firstErr) errorMsg = firstErr;
          }
        }
      }
      toast({ description: errorMsg });
      return null;
    }
  };

const Login = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const loginAsGuestUser = async (): Promise<void> => {
    try {
      const response: AxiosResponse = await customFetch.post('/auth/demo-login', {});
      const jwt: string = response.data.accessToken;
      const user = response.data.user;
      const username: string = user?.displayName || user?.userName || user?.email || 'Guest';
  dispatch(loginUser({ username, jwt }));
  // Fetch real user profile (demo user)
  await dispatch(getCurrentUserAsync());
  await dispatch(mergeLocalCartToServer());
  await dispatch(fetchCart());
      navigate('/');
    } catch (error) {
      let errorMsg = 'Login failed';
      if (error instanceof AxiosError) {
        const respData: unknown = error.response?.data;
        if (typeof respData === 'string') errorMsg = respData;
        else if (respData && typeof respData === 'object') {
          const obj = respData as { message?: string; errors?: Record<string, string[]> };
          if (obj.message) errorMsg = obj.message;
          else if (obj.errors) {
            const firstKey = Object.keys(obj.errors)[0];
            const firstErr = obj.errors[firstKey]?.[0];
            if (firstErr) errorMsg = firstErr;
          }
        }
      }
      toast({ description: errorMsg });
    }
  };
  return (
    <section className='h-screen grid place-items-center'>
      <Card className='w-96 bg-muted'>
        <CardHeader>
          <CardTitle className='text-center'>Login</CardTitle>
        </CardHeader>
        <CardContent>
          <Form method='post'>
            <FormInput type='email' label='email' name='email' />
            <FormInput type='password' name='password' />
            <SubmitBtn text='Login' className='w-full mt-4' />
            <Button
              type='button'
              variant='outline'
              onClick={loginAsGuestUser}
              className='w-full mt-4'
            >
              Guest User
            </Button>
            <p className='text-center mt-4'>
              Not a member yet?{' '}
              <Button type='button' asChild variant='link'>
                <Link to='/register'>Register</Link>
              </Button>
            </p>
          </Form>
        </CardContent>
      </Card>
    </section>
  );
}
export default Login;
