import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Check, ChevronRight, ShoppingBag } from 'lucide-react';
import { useGetProductQuery, useGetProductsMetaQuery, useGetProductsQuery } from '@/api/catalog';
import { isApiError } from '@/api/problem';
import { Loading, ProductCard, SelectProductAmount, SelectProductColor } from '@/components';
import Reveal from '@/components/Reveal';
import CompareToggle from '@/components/CompareToggle';
import DeliveryEstimate from '@/components/DeliveryEstimate';
import ProductGallery from '@/components/ProductGallery';
import RecentlyViewed from '@/components/RecentlyViewed';
import SaleBadge from '@/components/SaleBadge';
import WishlistButton from '@/components/WishlistButton';
import { Mode } from '@/components/SelectProductAmount';
import { Button } from '@/components/ui/button';
import { usePerks } from '@/content/perks';
import { useAddToBag } from '@/features/cart/useAddToBag';
import { useTrackProductView } from '@/features/recent/useRecentlyViewed';
import type { ProductDetail } from '@/api/types';
import { formatAsDollars, priceTag, type Product } from '@/utils';

const SingleProduct = () => {
  const { id } = useParams<{ id: string }>();
  // A card puts what the listing knows into the cache (no gallery); the page always asks for the rest
  const { data: product, isLoading, error } = useGetProductQuery(Number(id), { refetchOnMountOrArgChange: true });

  if (isLoading) return <Loading />;
  if (!product) {
    const notFound = isApiError(error) && error.status === 404;
    return (
      <div className="grid place-items-center py-24 text-center">
        <p className="eyebrow">{notFound ? '404' : 'Unavailable'}</p>
        <h1 className="display mt-4 text-5xl">{notFound ? 'Product not found' : 'Product unavailable'}</h1>
        <Button asChild variant="outline" className="mt-8">
          <Link to="/products">Back to the shop</Link>
        </Button>
      </div>
    );
  }
  // Keyed, so moving to another product starts with its own colour and amount
  return <ProductDetails key={product.id} product={product} />;
};

/** Up to four products from the same category, topped up from the same room. */
const RelatedProducts = ({ product }: { product: Product }) => {
  const group = product.groups[0];
  const { data: sameCategory } = useGetProductsQuery({ category: product.category, pageSize: 5 });
  const others = (sameCategory?.items ?? []).filter((p) => p.id !== product.id);
  const { data: sameGroup } = useGetProductsQuery({ group, pageSize: 8 }, { skip: !sameCategory || others.length >= 4 || !group });
  const related = [...others, ...(sameGroup?.items ?? []).filter((p) => p.id !== product.id && !others.some((o) => o.id === p.id))].slice(0, 4);

  if (related.length === 0) return null;
  return (
    <section className="mt-24 border-t pt-16 md:mt-32">
      <Reveal as="header">
        <p className="eyebrow">Keep browsing</p>
        <h2 className="display mt-3 text-4xl md:text-5xl">You may also like</h2>
      </Reveal>
      <div className="mt-10 grid gap-x-6 gap-y-12 sm:grid-cols-2 lg:grid-cols-4">
        {related.map((p, index) => (
          <Reveal key={p.id} delay={index * 80}>
            <ProductCard product={p} transition={false} />
          </Reveal>
        ))}
      </div>
    </section>
  );
};

const ProductDetails = ({ product }: { product: ProductDetail }) => {
  const { title, description, colors, company, widthCm, heightCm, depthCm, weightKg, materials, groups } = product;
  const materialsText = (materials ?? []).filter(Boolean).join(', ');
  const { price, effectivePrice, hasSale, percent } = priceTag(product);
  const [productColor, setProductColor] = useState(colors[0]);
  const [amount, setAmount] = useState(1);
  const [adding, setAdding] = useState(false);
  const [added, setAdded] = useState(false);
  const addToBag = useAddToBag();
  useTrackProductView(product.id);
  const perks = usePerks().filter((perk) => perk.key !== 'welcome');
  const { data: meta } = useGetProductsMetaQuery();
  const group = meta?.groupCategoryMap.find((g) => g.key === groups[0]);

  // The tick on the button fades back to the bag after a moment
  useEffect(() => {
    if (!added) return;
    const timer = window.setTimeout(() => setAdded(false), 1800);
    return () => window.clearTimeout(timer);
  }, [added]);

  const addToCart = async (event: React.MouseEvent<HTMLButtonElement>) => {
    const button = event.currentTarget;
    setAdding(true);
    try {
      // The cart charges what the page shows (the sale price on a sale); the bag slides in
      await addToBag(product, productColor, amount, button);
      setAdded(true);
    } catch {
      // Reported by the error middleware
    } finally {
      setAdding(false);
    }
  };

  const specs = [
    { label: 'Width', value: typeof widthCm === 'number' ? `${widthCm} cm` : null },
    { label: 'Height', value: typeof heightCm === 'number' ? `${heightCm} cm` : null },
    { label: 'Depth', value: typeof depthCm === 'number' ? `${depthCm} cm` : null },
    { label: 'Weight', value: typeof weightKg === 'number' ? `${weightKg} kg` : null },
  ].filter((spec) => spec.value);

  return (
    <>
      <nav aria-label="Breadcrumb" className="flex flex-wrap items-center gap-1.5 text-xs text-muted-foreground">
        <Link to="/" className="transition-colors hover:text-foreground">
          Home
        </Link>
        <ChevronRight className="h-3 w-3" />
        <Link to="/products" className="transition-colors hover:text-foreground">
          Shop
        </Link>
        {group && (
          <>
            <ChevronRight className="h-3 w-3" />
            <Link to={`/products?group=${encodeURIComponent(group.key)}`} className="transition-colors hover:text-foreground">
              {group.name}
            </Link>
          </>
        )}
        <ChevronRight className="h-3 w-3" />
        <span className="text-foreground">{title}</span>
      </nav>

      <section className="mt-8 grid gap-10 lg:grid-cols-12 lg:gap-16">
        <div className="lg:col-span-7">
          <div className="lg:sticky lg:top-24">
            <ProductGallery product={product} />
          </div>
        </div>

        <div className="lg:col-span-5">
          <p className="eyebrow animate-fade-up">{company}</p>
          <h1 className="display mt-3 animate-fade-up text-4xl leading-[1.02] [animation-delay:60ms] md:text-5xl">{title}</h1>
          <div className="mt-6 flex animate-fade-up flex-wrap items-center gap-3 [animation-delay:120ms]">
            <span className={hasSale ? 'text-3xl text-brand' : 'text-3xl'}>{formatAsDollars(effectivePrice)}</span>
            {hasSale && (
              <>
                <span className="text-lg text-muted-foreground line-through">{formatAsDollars(price)}</span>
                <SaleBadge percent={percent} />
              </>
            )}
          </div>
          <p className="mt-6 animate-fade-up leading-relaxed text-muted-foreground [animation-delay:180ms]">{description}</p>

          <div className="mt-8 grid animate-fade-up gap-8 border-t pt-8 [animation-delay:240ms]">
            <SelectProductColor colors={colors} productColor={productColor} setProductColor={setProductColor} />
            <div>
              <h4 className="text-[0.7rem] font-medium uppercase tracking-[0.14em] text-muted-foreground">Quantity</h4>
              <div className="mt-3 flex gap-3">
                <SelectProductAmount mode={Mode.SingleProduct} amount={amount} setAmount={setAmount} />
                <Button size="lg" className="group h-12 flex-1" onClick={addToCart} disabled={adding}>
                  <span className="relative h-4 w-4">
                    <ShoppingBag className={`absolute inset-0 transition-all duration-300 ${added ? 'scale-0 opacity-0' : 'scale-100 opacity-100'}`} />
                    <Check className={`absolute inset-0 transition-all duration-300 ${added ? 'scale-100 opacity-100' : 'scale-0 opacity-0'}`} />
                  </span>
                  Add to bag
                </Button>
                <WishlistButton product={product} className="h-12 w-12 shrink-0 border bg-transparent shadow-none" />
              </div>
              <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
                <DeliveryEstimate />
                <CompareToggle product={product} className="-mr-2.5" />
              </div>
            </div>
          </div>

          <ul className="mt-8 grid gap-3 rounded-2xl bg-secondary/60 p-5 text-sm">
            {perks.map(({ key, icon: Icon, title: perkTitle, description: perkText }) => (
              <li key={key} className="flex items-center gap-3">
                <Icon className="h-4 w-4 shrink-0" />
                <span>
                  <span className="font-medium">{perkTitle}</span> <span className="text-muted-foreground">— {perkText.charAt(0).toLowerCase() + perkText.slice(1)}</span>
                </span>
              </li>
            ))}
          </ul>

          {(specs.length > 0 || materialsText) && (
            <div className="mt-10">
              <h3 className="display text-2xl">Specifications</h3>
              <dl className="mt-4 grid grid-cols-2 border-t">
                {specs.map((spec) => (
                  <div key={spec.label} className="border-b py-4 odd:pr-4">
                    <dt className="eyebrow">{spec.label}</dt>
                    <dd className="mt-1 tabular-nums">{spec.value}</dd>
                  </div>
                ))}
                {materialsText && (
                  <div className="col-span-2 border-b py-4">
                    <dt className="eyebrow">Materials</dt>
                    <dd className="mt-1 capitalize">{materialsText}</dd>
                  </div>
                )}
              </dl>
            </div>
          )}
        </div>
      </section>

      <RelatedProducts product={product} />
      <RecentlyViewed excludeId={product.id} className="mt-20 border-t pt-10" />
    </>
  );
};
export default SingleProduct;
