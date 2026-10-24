import type { Product } from '@/utils';
import ProductCard from './ProductCard';
import Reveal from './Reveal';

/** The cards of the first row (three across on a wide screen): their pictures are what the page is judged by first. */
const FIRST_ROW = 3;

const ProductsGrid = ({ products }: { products: Product[] }) => (
  <div className="grid grid-cols-2 gap-x-3 gap-y-10 sm:gap-x-6 sm:gap-y-12 xl:grid-cols-3">
    {products.map((product, index) => (
      <Reveal key={product.id} delay={(index % 3) * 90}>
        <ProductCard product={product} priority={index < FIRST_ROW} />
      </Reveal>
    ))}
  </div>
);
export default ProductsGrid;
