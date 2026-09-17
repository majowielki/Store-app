import { Link } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui/card';
import { formatAsDollars, priceTag, type Product } from '@/utils';
import SaleBadge from './SaleBadge';

/** The price as every listing shows it: the sale price first, the list price struck through. */
export const ProductPrice = ({ product }: { product: Product }) => {
  const { price, effectivePrice, hasSale } = priceTag(product);
  return hasSale ? (
    <>
      <span className="text-primary font-semibold mr-2">{formatAsDollars(effectivePrice)}</span>
      <span className="line-through text-muted-foreground">{formatAsDollars(price)}</span>
    </>
  ) : (
    <span className="text-primary font-light">{formatAsDollars(price)}</span>
  );
};

/** A product tile of the grid listings (landing page, catalogue grid). */
const ProductCard = ({ product }: { product: Product }) => {
  const { title, image } = product;
  const { hasSale, percent } = priceTag(product);
  return (
    <Link to={`/products/${product.id}`}>
      <Card>
        <CardContent className="p-4">
          <div className="relative w-full aspect-[4/3] bg-gray-100 rounded-md overflow-hidden flex items-center justify-center">
            <img src={image} alt={title} className="w-full h-full object-cover" style={{ aspectRatio: '4/3' }} />
            {hasSale && <SaleBadge percent={percent} />}
          </div>
          <div className="mt-4 text-center">
            <h2 className="text-xl font-semibold capitalize">{title}</h2>
            <p className="mt-2">
              <ProductPrice product={product} />
            </p>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
};

export default ProductCard;
