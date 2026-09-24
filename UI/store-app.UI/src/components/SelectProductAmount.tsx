import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";

import { Mode } from "./selectProductAmountMode";

export { Mode };

interface SelectProductAmountProps {
  mode: Mode.SingleProduct;
  amount: number;
  setAmount: React.Dispatch<React.SetStateAction<number>>;
  className?: string;
};

interface SelectCartItemAmountProps {
  mode: Mode.CartItem;
  amount: number;
  setAmount: (value: number) => void;
  className?: string;
};

const SelectProductAmount = ({
  mode,
  amount,
  setAmount,
  className,
}: SelectProductAmountProps | SelectCartItemAmountProps) => {
  const cartItem = mode === Mode.CartItem;

  return (
    <Select
      defaultValue={amount.toString()}
      onValueChange={(value) => setAmount(Number(value))}
    >
      <SelectTrigger aria-label="Amount" className={cn(cartItem ? "h-9 w-[4.5rem] rounded-full" : "h-12 w-24 rounded-full", className)}>
        <SelectValue placeholder={amount} />
      </SelectTrigger>
      <SelectContent>
        {Array.from({ length: cartItem ? amount + 10 : 10 }, (_, index) => {
          const selectValue = (index + 1).toString();
          return (
            <SelectItem key={index} value={selectValue}>
              {selectValue}
            </SelectItem>
          );
        })}
      </SelectContent>
    </Select>
  );
}
export default SelectProductAmount;
