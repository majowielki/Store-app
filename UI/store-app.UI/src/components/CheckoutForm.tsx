import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { usePlaceOrderMutation } from '@/api/orders';
import { addressSaved } from '@/features/session/sessionSlice';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { toast } from '@/hooks/use-toast';
import FormCheckbox from './FormCheckbox';
import FormInput from './FormInput';
import SubmitBtn from './SubmitBtn';

const CheckoutForm = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const user = useAppSelector((state) => state.session.user);
  const [placeOrder, { isLoading }] = usePlaceOrderMutation();
  // One key per visit of the checkout page: a retry after a timeout or a double click
  // returns the order created the first time instead of charging twice
  const [idempotencyKey] = useState(() => crypto.randomUUID());

  const defaultUserName = user?.userName ?? '';
  const defaultAddress = user?.simpleAddress ?? '';
  // The demo accounts are shared by every visitor, so their profile stays as it is
  const isDemo = user?.isDemo ?? false;

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const customerName = String(formData.get('name') ?? '').trim();
    const deliveryAddress = String(formData.get('address') ?? '').trim();
    const saveAddress = !isDemo && formData.get('saveAddress') === 'on';

    if (!customerName || !deliveryAddress) {
      toast({ description: 'Please fill out all fields' });
      return;
    }

    try {
      await placeOrder({ order: { customerName, deliveryAddress, saveAddress }, idempotencyKey }).unwrap();
      // The identity service stores the address a moment later; the profile shown here is updated now
      if (saveAddress) dispatch(addressSaved(deliveryAddress));
      toast({ description: 'Order placed' });
      navigate('/orders');
    } catch {
      // Reported by the error middleware (an empty cart, a product that left the catalogue)
    }
  };

  return (
    <form method="post" className="flex flex-col gap-y-4" onSubmit={handleSubmit}>
      <h4 className="font-medium text-xl mb-4">Delivery Information</h4>
      <FormInput label="user name" name="name" type="text" defaultValue={defaultUserName} readOnly={isDemo} />
      <FormInput label="address" name="address" type="text" defaultValue={defaultAddress} readOnly={isDemo} />
      {!isDemo && <FormCheckbox name="saveAddress" label="save address to my profile" />}
      <SubmitBtn text="Place Your Order" className="mt-4" isSubmitting={isLoading} />
    </form>
  );
};

export default CheckoutForm;
