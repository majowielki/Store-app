/* eslint-disable react-refresh/only-export-components */
import { Form, Link, redirect } from 'react-router-dom';
import { Card, CardHeader, CardContent, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { SubmitBtn, FormInput } from '@/components';
// ...existing code...
import { store } from '@/store';
import { registerUserAsync, getCurrentUserAsync } from '@/features/user/userSlice';
import { mergeLocalCartToServer, fetchCart } from '@/features/cart/cartSlice';
import { toast } from '@/hooks/use-toast';
// ...existing code...

export const action = async ({ request }: { request: Request }): Promise<Response | null> => {
  const formData = await request.formData();
  const userData = {
    firstName: formData.get('firstName') as string,
    lastName: formData.get('lastName') as string,
    email: formData.get('email') as string,
    password: formData.get('password') as string,
    confirmPassword: formData.get('confirmPassword') as string,
  };
  try {
    // Use async thunk for real registration
    const result = await store.dispatch(registerUserAsync(userData));
    if (registerUserAsync.fulfilled.match(result)) {
      if (import.meta.env.VITE_USE_AUTH_ME === 'true') {
        await store.dispatch(getCurrentUserAsync());
      }
      await store.dispatch(mergeLocalCartToServer());
      await store.dispatch(fetchCart());
      toast({ description: 'Registered' });
      return redirect('/');
    } else {
      toast({ description: result.payload as string || 'Registration failed' });
      return null;
    }
  } catch {
    toast({ description: 'Registration failed' });
    return null;
  }
};

const Register = () => {
  const handleClose = () => {
    window.close();
  };
  return (
    <section className='h-screen grid place-items-center'>
      <Card className='w-96 bg-muted relative'>
        <button
          onClick={handleClose}
          className='absolute top-2 right-2 text-xl px-2 py-1 rounded hover:bg-gray-200'
          title='Zamknij stronę'
        >
          ×
        </button>
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