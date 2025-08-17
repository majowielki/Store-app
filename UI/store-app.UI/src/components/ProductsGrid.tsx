import { Link, useLoaderData } from "react-router-dom";
import { Card, CardContent } from "@/components/ui/card";
import { formatAsDollars, type ProductsResponse } from "@/utils";

const ProductsGrid = () => {
  const loaderData = useLoaderData() as ProductsResponse | undefined;
  const products = loaderData?.data ?? [];

  return (
    <div className="pt-12 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
  {products.map((product) => {
        const { title, price, image } = product.attributes;
        const salePrice = (product.attributes as unknown as { salePrice?: string | null }).salePrice ?? null;
        const hasSale = salePrice !== null && Number(salePrice) < Number(price);
        return (
          <Link to={`/products/${product.id}`} key={product.id}>
            <Card>
              <CardContent className="p-4">
                <img
                  src={image}
                  alt={title}
                  className="rounded-md h-64 md:h-48 w-full object-cover"
                />
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
  );
}
export default ProductsGrid;
