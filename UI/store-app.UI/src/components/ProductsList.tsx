import { useRef } from 'react';
import ResponsiveImage from '@/components/ResponsiveImage';
import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useOpenProduct } from '@/hooks/use-open-product';
import { priceTag, type Product } from '@/utils';
import { ColorDots, ProductPrice } from './ProductCard';
import Reveal from './Reveal';
import SaleBadge from './SaleBadge';
import { RatingLine } from './reviews/Stars';

const ProductRow = ({ product }: { product: Product }) => {
  const { title, image, company, description, colors } = product;
  const { hasSale, percent } = priceTag(product);
  const imageRef = useRef<HTMLImageElement>(null);
  const openProduct = useOpenProduct();

  return (
    <Link
      to={`/products/${product.id}`}
      viewTransition
      onClick={() => openProduct(product, imageRef.current)}
      className="group grid grid-cols-[7.5rem_1fr] items-center gap-5 py-5 sm:grid-cols-[13rem_1fr_auto] sm:gap-8"
    >
      <div className="relative aspect-5/4 overflow-hidden rounded-xl bg-muted">
        <ResponsiveImage size="tile" ref={imageRef} src={image} alt={title} loading="lazy" className="h-full w-full object-cover transition-transform duration-700 ease-smooth group-hover:scale-105" />
        {hasSale && <SaleBadge percent={percent} className="absolute left-2 top-2" />}
      </div>
      <div className="min-w-0">
        <p className="eyebrow">{company}</p>
        <h2 className="mt-1 text-lg font-medium">{title}</h2>
        <RatingLine average={product.ratingAverage} count={product.ratingCount} className="mt-1.5" />
        <div className="hidden sm:block">
          <p className="mt-2 line-clamp-2 max-w-xl text-sm text-muted-foreground">{description}</p>
        </div>
        <ColorDots colors={colors} className="mt-3" />
        <p className="mt-3 sm:hidden">
          <ProductPrice product={product} />
        </p>
      </div>
      <div className="hidden items-center gap-6 sm:flex">
        <ProductPrice product={product} stacked />
        <span className="grid h-11 w-11 place-items-center rounded-full border transition-colors duration-300 group-hover:border-foreground group-hover:bg-foreground group-hover:text-background">
          <ArrowUpRight className="h-5 w-5 transition-transform duration-500 ease-smooth group-hover:rotate-45" />
        </span>
      </div>
    </Link>
  );
};

const ProductsList = ({ products }: { products: Product[] }) => (
  <div className="divide-y border-y">
    {products.map((product, index) => (
      <Reveal key={product.id} delay={Math.min(index, 4) * 60}>
        <ProductRow product={product} />
      </Reveal>
    ))}
  </div>
);
export default ProductsList;
