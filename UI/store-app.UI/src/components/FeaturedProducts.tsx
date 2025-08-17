import { SectionTitle, SaleBadge } from ".";
import { useLoaderData } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui/card';
import { formatAsDollars, type ProductsResponse } from '@/utils';
import { Link } from 'react-router-dom';

const FeaturedProducts = () => {
  const loaderData = useLoaderData() as ProductsResponse | undefined;
  const products = loaderData?.data ?? [];
  // Temporary client-side filter for discounted products
  const toShow = products.slice(0, 6);
  return (
    <section className="pt-24">
  <SectionTitle text="discounted products" />
      <div className="pt-12 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
  {toShow.map((product) => {
          const { title, price, image } = product.attributes;
          // Backend sends price and salePrice as strings with 2 decimals
          const salePrice = (product.attributes as unknown as { salePrice?: string | null }).salePrice ?? null;
          const hasSale = salePrice !== null && Number(salePrice) < Number(price);
          const percent = hasSale
            ? ((Number(price) - Number(salePrice)) / Number(price)) * 100
            : 0;
          return (
            <Link to={`/products/${product.id}`} key={product.id}>
              <Card>
                <CardContent className="p-4">
                  <div className="relative">
                    <img
                      src={image}
                      alt={title}
                      className="rounded-md h-64 md:h-48 w-full object-cover"
                    />
                    {hasSale && <SaleBadge percent={percent} />}
                  </div>
                  <div className="mt-4 text-center">
                    <h2 className="text-xl font-semibold capitalize">{title}</h2>
                    <p className="mt-2">
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
