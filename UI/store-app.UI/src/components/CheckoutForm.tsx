/* eslint-disable react-refresh/only-export-components */
import { ActionFunction, Form, redirect } from 'react-router-dom';
import FormInput from './FormInput';
import SubmitBtn from './SubmitBtn';
import { customFetch } from '@/utils';
import { toast } from '@/hooks/use-toast';
import { clearCart } from '../features/cart/cartSlice';
import { ReduxStore } from '@/store';
import { useAppSelector } from '@/hooks';
import FormCheckbox from './FormCheckbox';

export const action =
  (store: ReduxStore): ActionFunction =>
  async ({ request }): Promise<null | Response> => {
    const formData = await request.formData();
    const name = formData.get('name') as string;
    const address = formData.get('address') as string;
  const saveAddress = formData.get('saveAddress') === 'on';

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
  // Address saving is now handled by the order API using saveAddress

      await customFetch.post('/orders/from-cart', {
        // userId is set on the server from JWT; still include for DTO validation
        userId: user.id,
        userEmail,
        deliveryAddress,
        customerName,
        notes: undefined,
        saveAddress,
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
  const user = useAppSelector((state) => state.userState.user);
  const defaultUserName = user?.userName ?? '';
  const defaultAddress = user?.simpleAddress ?? '';
  const hiddenCheckboxUsers = ["demo@store.com", "demo-admin@store.com"];
  const shouldShowCheckbox = user && !hiddenCheckboxUsers.includes(user.email);
  const isDemo = !!(user && hiddenCheckboxUsers.includes(user.email));
  return (
    <Form method="post" className="flex flex-col gap-y-4">
      <h4 className="font-medium text-xl mb-4">Delivery Information</h4>
      <FormInput label="user name" name="name" type="text" defaultValue={defaultUserName} disabled={isDemo} />
      {isDemo && (
        <input type="hidden" name="name" value={defaultUserName} />
      )}
      <FormInput label="address" name="address" type="text" defaultValue={defaultAddress} disabled={isDemo} />
      {isDemo && (
        <input type="hidden" name="address" value={defaultAddress} />
      )}
      {shouldShowCheckbox && (
        <FormCheckbox name="saveAddress" label="save address to my profile" />
      )}
      <SubmitBtn text="Place Your Order" className="mt-4" />
    </Form>
  );
};

export default CheckoutForm;