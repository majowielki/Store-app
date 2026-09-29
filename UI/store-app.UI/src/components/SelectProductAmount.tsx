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

interface AmountLabel {
  /** What the choice is called for a screen reader ("Quantity"); the number chosen is read after it. */
  label: string;
}

interface SelectProductAmountProps extends AmountLabel {
  mode: Mode.SingleProduct;
  amount: number;
  setAmount: React.Dispatch<React.SetStateAction<number>>;
  /** The most a customer can take: the units still available, when fewer than ten. */
  max?: number;
  className?: string;
};

interface SelectCartItemAmountProps extends AmountLabel {
  mode: Mode.CartItem;
  amount: number;
  setAmount: (value: number) => void;
  className?: string;
};

const SelectProductAmount = (props: SelectProductAmountProps | SelectCartItemAmountProps) => {
  const { mode, amount, setAmount, label, className } = props;
  const cartItem = mode === Mode.CartItem;
  const max = props.mode === Mode.SingleProduct ? props.max : undefined;
  const choices = cartItem ? amount + 10 : Math.max(1, Math.min(10, max ?? 10));

  return (
    <Select
      defaultValue={amount.toString()}
      onValueChange={(value) => setAmount(Number(value))}
    >
      {/* The button shows only the number, so its name is the label followed by that number */}
      <SelectTrigger aria-label={`${label}, ${amount}`} className={cn(cartItem ? "h-9 w-18 rounded-full" : "h-12 w-24 rounded-full", className)}>
        <SelectValue placeholder={amount} />
      </SelectTrigger>
      <SelectContent>
        {Array.from({ length: choices }, (_, index) => {
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
