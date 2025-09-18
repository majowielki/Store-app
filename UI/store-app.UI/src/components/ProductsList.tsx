import { formatAsDollars, type ProductsResponse } from "@/utils";
import { Link, useLoaderData } from "react-router-dom";
import { Card, CardContent } from "./ui/card";
import { Badge } from "./ui/badge";

const ProductsList = () => {
  const { data: products } = useLoaderData() as ProductsResponse;

  return (
  <div className="mt-6 grid gap-y-4">
      {products.map((product) => {
        const { title, price, image, company } = product.attributes;
        const salePrice = (product.attributes as unknown as { salePrice?: string | null }).salePrice ?? null;
        const hasSale = salePrice !== null && Number(salePrice) < Number(price);
        let discountPercent = null;
        if (hasSale) {
          const discount = ((Number(price) - Number(salePrice)) / Number(price)) * 100;
          discountPercent = Math.round(discount);
        }
        return (
          <Link key={product.id} to={`/products/${product.id}`}>
            <Card>
              <CardContent className="p-4 gap-y-2 grid md:grid-cols-3">
                <div className="w-full md:w-64 aspect-[4/3] bg-gray-100 rounded-lg overflow-hidden flex items-center justify-center relative">
                  <img
                    src={image}
                    alt={title}
                    width={1280}
                    height={960}
                    className="w-full h-full object-cover"
                    style={{ aspectRatio: '4/3' }}
                  />
                  {discountPercent && (
                    <Badge className="absolute top-2 left-2 bg-red-500 text-white shadow-md z-10">
                      -{discountPercent}%
                    </Badge>
                  )}
                </div>
                <div>
                  <h2 className="text-xl font-semibold capitalize">{title}</h2>
                  <h4>{company}</h4>
                </div>
                <p className="md:ml-auto mt-2 md:mt-0">
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
              </CardContent>
            </Card>
          </Link>
        );
      })}
    </div>
  );
}
export default ProductsList;
