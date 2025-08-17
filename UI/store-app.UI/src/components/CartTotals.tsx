import { useAppSelector } from "@/hooks";
import { formatAsDollars } from "@/utils";
import { Card, CardTitle } from "@/components/ui/card";
import { Separator } from "./ui/separator";

interface CartTotalRowProps {
  label: string;
  amount: number;
  lastRow?: boolean;
}

const CartTotalRow = ({ label, amount, lastRow }: CartTotalRowProps) => {
  return (
    <>
      <p className="flex justify-between text-sm">
        <span>{label}</span>
        <span>{formatAsDollars(amount)}</span>
      </p>
      {lastRow ? null : <Separator className="my-2" />}
    </>
  );
};

const CartTotals = () => {
  const { cartTotal, tax, orderTotal } = useAppSelector(
    (state) => state.cartState
  );
  return (
    <Card className="p-8 bg-muted">
      <CartTotalRow label="Subtotal" amount={cartTotal} />
      <CartTotalRow label="Delivery" amount={tax} />
      <CardTitle className="mt-8">
        <CartTotalRow label="Order Total" amount={orderTotal} lastRow />
      </CardTitle>
    </Card>
  );
};
export default CartTotals;
