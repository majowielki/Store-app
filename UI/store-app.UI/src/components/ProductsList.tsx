import { Link } from 'react-router-dom';
import { priceTag, type Product } from '@/utils';
import { Badge } from './ui/badge';
import { Card, CardContent } from './ui/card';
import { ProductPrice } from './ProductCard';

const ProductsList = ({ products }: { products: Product[] }) => (
  <div className="mt-6 grid gap-y-4">
    {products.map((product) => {
      const { title, image, company } = product;
      const { hasSale, percent } = priceTag(product);
      return (
        <Link key={product.id} to={`/products/${product.id}`}>
          <Card>
            <CardContent className="p-4 gap-y-2 grid md:grid-cols-3">
              <div className="w-full md:w-64 aspect-[4/3] bg-gray-100 rounded-lg overflow-hidden flex items-center justify-center relative">
                <img src={image} alt={title} className="w-full h-full object-cover" style={{ aspectRatio: '4/3' }} />
                {hasSale && (
                  <Badge className="absolute top-2 left-2 bg-red-500 text-white shadow-md z-10">-{percent}%</Badge>
                )}
              </div>
              <div>
                <h2 className="text-xl font-semibold capitalize">{title}</h2>
                <h4 className="capitalize">{company}</h4>
              </div>
              <p className="md:ml-auto mt-2 md:mt-0">
                <ProductPrice product={product} />
              </p>
            </CardContent>
          </Card>
        </Link>
      );
    })}
  </div>
);
export default ProductsList;
