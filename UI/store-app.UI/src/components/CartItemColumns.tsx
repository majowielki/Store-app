import { formatAsDollars } from "@/utils";
import { useAppDispatch } from "@/hooks";
import { Button } from "./ui/button";
import { editItem, removeItem } from "@/features/cart/cartSlice";
import SelectProductAmount from "./SelectProductAmount";
import { Mode } from "./SelectProductAmount";

interface FirstColumnProps {
  image: string;
  title: string;
}

interface SecondColumnProps {
  title: string;
  company: string;
  productColor: string;
  price: string;
}

interface ThirdColumnProps {
  amount: number;
  cartID: string;
}

interface FourthColumnProps {
  price: string;
  amount: number;
}

export const FirstColumn = ({ title, image }: FirstColumnProps) => {
  return (
    <img
      src={image}
      alt={title}
      className="h-24 w-24 rounded-lg sm:h-32 sm:w-32 object-cover"
    />
  );
};

export const SecondColumn = ({
  title,
  company,
  productColor,
  price,
}: SecondColumnProps) => {
  return (
    <div className="sm:ml-4 md:ml-12 sm:w-48">
      <h3 className="capitalize font-medium">{title}</h3>
      <h4 className="mt-3 capitalize text-sm">{company}</h4>
      <p className="mt-4 text-sm capitalize flex items-center gap-x-2">
        color :{" "}
        <span
          style={{
            width: "15px",
            height: "15px",
            borderRadius: "50%",
            background: productColor,
          }}
        ></span>
      </p>
      <p className="mt-4 text-sm">Price: {formatAsDollars(price)}</p>
    </div>
  );
};

export const ThirdColumn = ({ amount, cartID }: ThirdColumnProps) => {
  const dispatch = useAppDispatch();

  const removeItemFromCart = () => {
    dispatch(removeItem(cartID));
  };

  const setAmount = (value: number) => {
    dispatch(editItem({ cartID, amount: value }));
  };
  return (
    <div>
      <SelectProductAmount
        amount={amount}
        setAmount={setAmount}
        mode={Mode.CartItem}
      />
      <Button variant="link" className="-ml-4" onClick={removeItemFromCart}>
        remove
      </Button>
    </div>
  );
};

export const FourthColumn = ({ price, amount }: FourthColumnProps) => {
  const totalPrice = (parseFloat(price) * amount).toFixed(2);
  return (
    <div className="sm:ml-auto">
      <p className="font-medium">Total price:</p>
      <p>{formatAsDollars(totalPrice)}</p>
    </div>
  );
};