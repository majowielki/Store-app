import { SectionTitle, SaleBadge } from ".";
import { useLoaderData } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui/card';
import { formatAsDollars, priceTag, type ProductsResponse } from '@/utils';
import { Link } from 'react-router-dom';

const FeaturedProducts = () => {
  const loaderData = useLoaderData() as ProductsResponse | undefined;
  const products = loaderData?.items ?? [];
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
      <div className="pt-12 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {toShow.map((product) => {
          const { title, image } = product;
          const { price, effectivePrice, hasSale, percent } = priceTag(product);
          return (
            <Link to={`/products/${product.id}`} key={product.id}>
              <Card>
                <CardContent className="p-4">
                  <div className="relative w-full aspect-[4/3] bg-gray-100 rounded-md overflow-hidden flex items-center justify-center">
                    <img
                      src={image}
                      alt={title}
                      width={1184}
                      height={896}
                      className="w-full h-full object-cover"
                      style={{ aspectRatio: '4/3' }}
                    />
                    {hasSale && <SaleBadge percent={percent} />}
                  </div>
                  <div className="mt-4 text-center">
                    <h2 className="text-xl font-semibold capitalize">{title}</h2>
                    <p className="mt-2">
                      {hasSale ? (
                        <>
                          <span className="text-primary font-semibold mr-2">
                            {formatAsDollars(effectivePrice)}
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
                  </div>
                </CardContent>
              </Card>
            </Link>
          );
        })}
      </div>
    </section>
  );
}
export default FeaturedProducts;
