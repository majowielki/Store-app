import { useGetProductsQuery } from '@/api/catalog';
import { priceTag } from '@/utils';
import { Loading, SectionTitle } from '.';
import ProductCard from './ProductCard';

const FeaturedProducts = () => {
  const { data, isLoading } = useGetProductsQuery({ sale: 'true' });
  const products = data?.items ?? [];
  // Show up to 6 discounted products, filled up with others when there are fewer
  const discounted = products.filter((product) => priceTag(product).hasSale);
  let toShow = discounted.slice(0, 6);
  if (toShow.length < 6) {
    const nonDiscounted = products.filter((p) => !discounted.includes(p));
    toShow = toShow.concat(nonDiscounted.slice(0, 6 - toShow.length));
  }
  return (
    <section className="pt-12">
      <SectionTitle text="discounted products" />
      {isLoading ? (
        <Loading />
      ) : (
        <div className="pt-12 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {toShow.map((product) => (
            <ProductCard key={product.id} product={product} />
          ))}
        </div>
      )}
    </section>
  );
};
export default FeaturedProducts;
