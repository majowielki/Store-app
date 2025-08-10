/* eslint-disable react-refresh/only-export-components */
import { ActionFunction, Form, redirect } from 'react-router-dom';
import FormInput from './FormInput';
import SubmitBtn from './SubmitBtn';
import { customFetch } from '@/utils';
import { toast } from '@/hooks/use-toast';
import { clearCart } from '../features/cart/cartSlice';
import { ReduxStore } from '@/store';

export const action =
  (store: ReduxStore): ActionFunction =>
  async ({ request }): Promise<null | Response> => {
    const formData = await request.formData();
    const name = formData.get('name') as string;
    const address = formData.get('address') as string;

    if (!name || !address) {
      toast({ description: 'please fill out all fields' });
      return null;
    }
    const user = store.getState().userState.user;
    if (!user) {
      toast({ description: 'please login to place an order' });
      return redirect('/login');
    }

  const customerName = name;
  const deliveryAddress = address;
  const userEmail = user.email;

    try {
      await customFetch.post('/orders/from-cart', {
        // userId is set on the server from JWT; still include for DTO validation
        userId: user.id,
        userEmail,
        deliveryAddress,
        customerName,
        notes: undefined,
      });

      store.dispatch(clearCart());
      toast({ description: 'order placed' });
      return redirect('/orders');
  } catch {
      toast({ description: 'order failed' });
      return null;
    }
  };

const CheckoutForm = () => {
  return (
    <Form method="post" className="flex flex-col gap-y-4">
      <h4 className="font-medium text-xl mb-4">Shipping Information</h4>
      <FormInput label="first name" name="name" type="text" />
      <FormInput label="address" name="address" type="text" />
      <SubmitBtn text="Place Your Order" className="mt-4" />
    </Form>
  );
};

export default CheckoutForm;