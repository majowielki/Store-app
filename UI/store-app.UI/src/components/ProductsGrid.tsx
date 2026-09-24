import type { Product } from '@/utils';
import ProductCard from './ProductCard';
import Reveal from './Reveal';

const ProductsGrid = ({ products }: { products: Product[] }) => (
  <div className="grid gap-x-6 gap-y-12 sm:grid-cols-2 xl:grid-cols-3">
    {products.map((product, index) => (
      <Reveal key={product.id} delay={(index % 3) * 90}>
        <ProductCard product={product} />
      </Reveal>
    ))}
  </div>
);
export default ProductsGrid;
