import { useLoaderData } from "react-router-dom";
import { Ruler, Scale, Layers } from "lucide-react";
import { Link, type LoaderFunction } from "react-router-dom";
import {
  customFetch,
  formatAsDollars,
  type SingleProductResponse,
  type CartItem,
} from "@/utils";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { SelectProductAmount, SelectProductColor } from "@/components";
import { Mode } from "@/components/SelectProductAmount";
import { useAppDispatch, useAppSelector } from "@/hooks";
import { addItem, addItemToServer } from "@/features/cart/cartSlice";

// eslint-disable-next-line react-refresh/only-export-components
export const loader: LoaderFunction = async ({
  params,
}): Promise<SingleProductResponse> => {
  const response = await customFetch<SingleProductResponse>(
    `/products/${params.id}`
  );
  return { ...response.data };
};

const SingleProduct = () => {
  const { data: product } = useLoaderData() as SingleProductResponse;
  const { image, title, price, description, colors, company } = product.attributes;
  const companyLabel = company ? company.charAt(0).toUpperCase() + company.slice(1) : '';
  const { widthCm, heightCm, depthCm, weightKg, materials } = product.attributes as unknown as {
    widthCm?: number | null;
    heightCm?: number | null;
    depthCm?: number | null;
    weightKg?: number | null;
    materials?: string | null;
  };
  // Safe materials display in case backend sends non-string values
  let materialsText = '' as string;
  if (typeof (materials as unknown) === 'string') {
    materialsText = (materials as unknown as string).trim();
  } else if (Array.isArray(materials)) {
    materialsText = (materials as unknown as unknown[]).filter(Boolean).join(', ');
  }
  const salePrice = (product.attributes as unknown as { salePrice?: string | null }).salePrice ?? null;
  const hasSale = salePrice !== null && Number(salePrice) < Number(price);
  const [productColor, setProductColor] = useState(colors[0]);
  const [amount, setAmount] = useState(1);
  const dispatch = useAppDispatch();
  const user = useAppSelector((s) => s.userState.user);
  const cartProduct: CartItem = {
    cartID: product.id + productColor,
    productID: product.id,
    image,
    title,
    price,
    amount,
    productColor,
    company,
  };

  const addToCart = async () => {
    if (user) {
      try {
        await dispatch(
          addItemToServer({
            productId: product.id,
            quantity: amount,
            color: productColor,
          })
        ).unwrap();
      } catch {
        // Fallback to local cart when server rejects (e.g., 401)
        dispatch(addItem(cartProduct));
      }
    } else {
      dispatch(addItem(cartProduct));
    }
  };

  return (
    <section>
      <div className="flex gap-x-2 h-6 items-center">
        <Button asChild variant="link" size="sm">
          <Link to="/">Home</Link>
        </Button>
        <Separator orientation="vertical" />
        <Button asChild variant="link" size="sm">
          <Link to="/products">Products</Link>
        </Button>
      </div>
      {/* PRODUCT */}
      <div className="mt-6 grid gap-y-8 lg:grid-cols-2 lg:gap-x-16">
        {/* IMAGE FIRST COL */}
        <img
          src={image}
          alt={title}
          className="w-96 h-96 object-cover rounded-lg lg:w-full"
        />
        {/* PRODUCT INFO SECOND COL */}
        <div>
          <h1 className="capitalize text-3xl font-bold">{title}</h1>
          <h4 className="text-xl mt-2">{companyLabel}</h4>
          <p className="mt-3 text-md bg-muted inline-block p-2 rounded-md">
            {hasSale ? (
              <>
                <span className="text-primary font-semibold mr-2">
                  {formatAsDollars(salePrice)}
                </span>
                <span className="line-through text-muted-foreground">
                  {formatAsDollars(price)}
                </span>
              </>
            ) : (
              <span className="text-primary font-light">
                {formatAsDollars(price)}
              </span>
            )}
          </p>
          <p className="mt-6 leading-8">{description}</p>

          {(widthCm ?? heightCm ?? depthCm ?? weightKg ?? materialsText) && (
            <div className="mt-6 border rounded-md p-4 bg-muted/40">
              <h3 className="font-semibold mb-3">Specifications</h3>
              <ul className="grid gap-3 text-sm md:grid-cols-2 lg:grid-cols-3">
                {typeof widthCm === 'number' && (
                  <li className="flex items-center gap-2">
                    <Ruler className="h-4 w-4 text-muted-foreground" />
                    <span className="text-muted-foreground">Width:</span> {widthCm} cm
                  </li>
                )}
                {typeof heightCm === 'number' && (
                  <li className="flex items-center gap-2">
                    <Ruler className="h-4 w-4 text-muted-foreground" />
                    <span className="text-muted-foreground">Height:</span> {heightCm} cm
                  </li>
                )}
                {typeof depthCm === 'number' && (
                  <li className="flex items-center gap-2">
                    <Ruler className="h-4 w-4 text-muted-foreground" />
                    <span className="text-muted-foreground">Depth:</span> {depthCm} cm
                  </li>
                )}
                {typeof weightKg === 'number' && (
                  <li className="flex items-center gap-2">
                    <Scale className="h-4 w-4 text-muted-foreground" />
                    <span className="text-muted-foreground">Weight:</span> {weightKg} kg
                  </li>
                )}
        {materialsText && (
                  <li className="flex items-center gap-2 md:col-span-2 lg:col-span-3">
                    <Layers className="h-4 w-4 text-muted-foreground" />
          <span className="text-muted-foreground">Materials:</span> {materialsText}
                  </li>
                )}
              </ul>
            </div>
          )}
          {/* COLORS  */}
          <SelectProductColor
            colors={colors}
            productColor={productColor}
            setProductColor={setProductColor}
          />

          {/* AMOUNT  */}
          <SelectProductAmount
            mode={Mode.SingleProduct}
            amount={amount}
            setAmount={setAmount}
          />
          {/* CART BUTTON  */}
          <Button size="lg" className="mt-10" onClick={addToCart}>
            Add to bag
          </Button>
        </div>
      </div>
    </section>
  );
}
export default SingleProduct;
