import type { Product } from '@/utils';
import ProductCard from './ProductCard';

const ProductsGrid = ({ products }: { products: Product[] }) => (
  <div className="pt-12 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
    {products.map((product) => (
      <ProductCard key={product.id} product={product} />
    ))}
  </div>
);
export default ProductsGrid;
