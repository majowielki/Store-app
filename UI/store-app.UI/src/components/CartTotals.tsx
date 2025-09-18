
import { useAppSelector } from "@/hooks";
import { formatAsDollars } from "@/utils";
import { Card, CardTitle } from "@/components/ui/card";
import { Separator } from "./ui/separator";
import { useEffect, useState } from "react";
import { orderApi } from "@/utils/api";

interface CartTotalRowProps {
  label: string;
  amount: number;
  lastRow?: boolean;
  isDiscount?: boolean;
}

const CartTotalRow = ({ label, amount, lastRow, isDiscount }: CartTotalRowProps) => {
  return (
    <>
      <p className={`flex justify-between text-sm ${isDiscount ? 'text-green-700' : ''}`}>
        <span>{label}</span>
        <span>{isDiscount ? '-' : ''}{formatAsDollars(Math.abs(amount))}</span>
      </p>
      {lastRow ? null : <Separator className="my-2" />}
    </>
  );
};

const CartTotals = () => {
  const { cartTotal, tax, orderTotal } = useAppSelector((state) => state.cartState);
  const user = useAppSelector((state) => state.userState.user);
  const [isFirstOrder, setIsFirstOrder] = useState(false);

  useEffect(() => {
    const checkFirstOrder = async () => {
      if (!user) {
        setIsFirstOrder(false);
        return;
      }
  // loading state removed
      try {
        const res = await orderApi.getHasOrders();
        setIsFirstOrder(res.ordersCount === 0);
      } catch {
        setIsFirstOrder(false);
      } finally {
        // loading state removed
      }
    };
    checkFirstOrder();
  }, [user]);

  let discount = 0;
  if (isFirstOrder && cartTotal > 0) {
    discount = cartTotal * 0.2;
  }
  const totalAfterDiscount = cartTotal - discount + tax;

  return (
    <Card className="p-8 bg-muted">
      <CartTotalRow label="Subtotal" amount={cartTotal} />
      {isFirstOrder && discount > 0 && (
        <CartTotalRow label="First order discount" amount={-discount} isDiscount />
      )}
      <CartTotalRow label="Delivery" amount={tax} />
      <CardTitle className="mt-8">
        <CartTotalRow label="Order Total" amount={isFirstOrder ? totalAfterDiscount : orderTotal} lastRow />
      </CardTitle>
    </Card>
  );
};
export default CartTotals;
