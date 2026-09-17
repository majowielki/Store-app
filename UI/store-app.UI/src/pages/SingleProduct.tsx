import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Layers, Ruler, Scale } from 'lucide-react';
import { useGetProductQuery } from '@/api/catalog';
import { isApiError } from '@/api/problem';
import { Loading, SectionTitle, SelectProductAmount, SelectProductColor } from '@/components';
import { Mode } from '@/components/SelectProductAmount';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { useCartActions } from '@/features/cart/useCart';
import { toast } from '@/hooks/use-toast';
import { formatAsDollars, priceTag, type Product } from '@/utils';

const SingleProduct = () => {
  const { id } = useParams<{ id: string }>();
  const { data: product, isLoading, error } = useGetProductQuery(Number(id));

  if (isLoading) return <Loading />;
  if (!product) {
    return <SectionTitle text={isApiError(error) && error.status === 404 ? 'Product not found' : 'Product unavailable'} />;
  }
  return <ProductDetails product={product} />;
};

const ProductDetails = ({ product }: { product: Product }) => {
  const { image, title, description, colors, company, widthCm, heightCm, depthCm, weightKg, materials } = product;
  const companyLabel = company ? company.charAt(0).toUpperCase() + company.slice(1) : '';
  const materialsText = (materials ?? []).filter(Boolean).join(', ');
  const { price, effectivePrice, hasSale } = priceTag(product);
  const [productColor, setProductColor] = useState(colors[0]);
  const [amount, setAmount] = useState(1);
  const { add } = useCartActions();

  const addToCart = async () => {
    try {
      // The cart charges what the page shows: the sale price when the product is on sale
      await add({ productId: product.id, title, image, company, color: productColor, unitPrice: effectivePrice, quantity: amount });
      toast({ description: 'Item added to cart' });
    } catch {
      // Reported by the error middleware
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
      <div className="mt-6 grid gap-y-8 lg:grid-cols-2 lg:gap-x-16">
        <div className="w-full max-w-[500px] mx-auto aspect-[4/3] bg-gray-100 rounded-lg overflow-hidden flex items-center justify-center sm:max-w-[400px] lg:max-w-full">
          <img src={image} alt={title} className="w-full h-full object-cover" style={{ aspectRatio: '4/3' }} />
        </div>
        <div>
          <h1 className="capitalize text-3xl font-bold">{title}</h1>
          <h4 className="text-xl mt-2">{companyLabel}</h4>
          <p className="mt-3 text-md bg-muted inline-block p-2 rounded-md">
            {hasSale ? (
              <>
                <span className="text-primary font-semibold mr-2">{formatAsDollars(effectivePrice)}</span>
                <span className="line-through text-muted-foreground">{formatAsDollars(price)}</span>
              </>
            ) : (
              <span className="text-primary font-light">{formatAsDollars(price)}</span>
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
          <SelectProductColor colors={colors} productColor={productColor} setProductColor={setProductColor} />
          <SelectProductAmount mode={Mode.SingleProduct} amount={amount} setAmount={setAmount} />
          <Button size="lg" className="mt-10" onClick={addToCart}>
            Add to bag
          </Button>
        </div>
      </div>
    </section>
  );
};
export default SingleProduct;
