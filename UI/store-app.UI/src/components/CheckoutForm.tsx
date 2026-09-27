import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { usePlaceOrderMutation } from '@/api/orders';
import { codeRemoved } from '@/features/cart/discountCodeSlice';
import { useDiscountCode } from '@/features/cart/useDiscountCode';
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
  const discount = useDiscountCode();
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
      // Only a code the order service accepted goes with the order; it is checked once more there
      const discountCode = discount.check?.code;
      await placeOrder({ order: { customerName, deliveryAddress, saveAddress, discountCode }, idempotencyKey }).unwrap();
      // The identity service stores the address a moment later; the profile shown here is updated now
      if (saveAddress) dispatch(addressSaved(deliveryAddress));
      dispatch(codeRemoved());
      toast({ description: 'Order placed' });
      navigate('/orders');
    } catch {
      // Reported by the error middleware (an empty cart, a product that left the catalogue)
    }
  };

  return (
    <form method="post" className="flex flex-col gap-y-5" onSubmit={handleSubmit}>
      <div className="mb-2">
        <h2 className="display text-3xl">Delivery details</h2>
        <p className="mt-2 text-sm text-muted-foreground">Where should we bring your order? We will call before we come.</p>
      </div>
      <FormInput label="user name" name="name" type="text" defaultValue={defaultUserName} readOnly={isDemo} />
      <FormInput label="address" name="address" type="text" defaultValue={defaultAddress} readOnly={isDemo} />
      {!isDemo && <FormCheckbox name="saveAddress" label="save address to my profile" />}
      <SubmitBtn text="Place Your Order" className="mt-4 w-full" isSubmitting={isLoading} />
    </form>
  );
};

export default CheckoutForm;
