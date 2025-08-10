/* eslint-disable react-refresh/only-export-components */
import { ActionFunction, Form, Link, redirect } from 'react-router-dom';
import { Card, CardHeader, CardContent, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { SubmitBtn, FormInput } from '@/components';
import { customFetch } from '@/utils';
import { store } from '@/store';
import { loginUser, getCurrentUserAsync } from '@/features/user/userSlice';
import { mergeLocalCartToServer, fetchCart } from '@/features/cart/cartSlice';
import { toast } from '@/hooks/use-toast';
import { AxiosError } from 'axios';

export const action: ActionFunction = async ({
  request,
}): Promise<Response | null> => {
  const formData = await request.formData();
  const data = Object.fromEntries(formData);
  try {
    const response = await customFetch.post('/auth/register', data);
    // Auto-login: store token and fetch profile
    const jwt: string | undefined = response.data?.accessToken;
    const user = response.data?.user;
    if (jwt) {
      store.dispatch(loginUser({ username: user?.displayName || user?.userName || user?.email || 'User', jwt }));
      store.dispatch(getCurrentUserAsync());
      await store.dispatch(mergeLocalCartToServer());
      await store.dispatch(fetchCart());
    }
    toast({ description: 'Registered' });
    return redirect('/');
  } catch (error) {
    let errorMsg = 'Registration failed';
    if (error instanceof AxiosError) {
      const respData: unknown = error.response?.data;
      if (typeof respData === 'string') {
        errorMsg = respData;
      } else if (respData && typeof respData === 'object') {
        const maybeObj = respData as { message?: string; errors?: Record<string, string[]> };
        if (maybeObj.message) {
          errorMsg = maybeObj.message;
        } else if (maybeObj.errors) {
          const keys = Object.keys(maybeObj.errors);
          const firstKey = keys[0];
          const firstErr = maybeObj.errors[firstKey]?.[0];
          if (firstErr) errorMsg = firstErr;
        }
      }
    }
    toast({ description: errorMsg });

    return null;
  }
};

const Register = () => {
  return (
    <section className='h-screen grid place-items-center'>
      <Card className='w-96 bg-muted'>
        <CardHeader>
          <CardTitle className='text-center'>Register</CardTitle>
        </CardHeader>
        <CardContent>
          <Form method='post'>
            <FormInput type='text' name='firstName' />
            <FormInput type='text' name='lastName' />
            <FormInput type='email' name='email' />
            <FormInput type='password' name='password' />
            <FormInput type='password' name='confirmPassword' />
            <SubmitBtn text='Register' className='w-full mt-4' />
            <p className='text-center mt-4'>
              Already a member?{' '}
              <Button type='button' asChild variant='link'>
                <Link to='/login'>Login</Link>
              </Button>
            </p>
          </Form>
        </CardContent>
      </Card>
    </section>
  );
}
export default Register;