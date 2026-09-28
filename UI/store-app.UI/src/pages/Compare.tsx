import type { ReactNode } from 'react';
import ResponsiveImage from '@/components/ResponsiveImage';
import { Link } from 'react-router-dom';
import { ArrowRight, Scale, ShoppingBag, X } from 'lucide-react';
import Loading from '@/components/Loading';
import ProductPrice from '@/components/ProductPrice';
import FinishLabel from '@/components/swatch/FinishLabel';
import { Button } from '@/components/ui/button';
import { useAddToBag } from '@/features/cart/useAddToBag';
import { compareCleared, removedFromCompare } from '@/features/compare/compareSlice';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { useProductsById } from '@/hooks/use-products-by-id';
import { usePageMeta } from '@/seo';
import type { Product } from '@/utils';

const cm = (value?: number | null) => (typeof value === 'number' ? `${value} cm` : '—');

/** The rows of the comparison: a label and what each product has for it. */
const rows: { label: string; value: (product: Product) => ReactNode }[] = [
  { label: 'Price', value: (product) => <ProductPrice product={product} /> },
  { label: 'Maker', value: (product) => <span className="capitalize">{product.company}</span> },
  { label: 'Width', value: (product) => cm(product.widthCm) },
  { label: 'Height', value: (product) => cm(product.heightCm) },
  { label: 'Depth', value: (product) => cm(product.depthCm) },
  { label: 'Weight', value: (product) => (typeof product.weightKg === 'number' ? `${product.weightKg} kg` : '—') },
  { label: 'Materials', value: (product) => <span className="capitalize">{(product.materials ?? []).join(', ') || '—'}</span> },
  {
    label: 'Colours',
    value: (product) => (
      <span className="flex flex-wrap items-center gap-1.5">
        {product.colors.map((color) => (
          <FinishLabel key={color} color={color} className="text-xs" />
        ))}
      </span>
    ),
  },
];

/**
 * The pieces picked for comparison side by side: price, measurements, weight, materials and
 * colours. On a phone the table scrolls sideways with the row names held in place.
 */
const Compare = () => {
  usePageMeta({ title: 'Compare', noindex: true });
  const dispatch = useAppDispatch();
  const ids = useAppSelector((state) => state.compare.productIds);
  const { products, isLoading } = useProductsById(ids);
  const addToBag = useAddToBag();

  if (isLoading) return <Loading />;

  if (products.length === 0) {
    return (
      <div className="grid place-items-center py-16 text-center md:py-24">
        <span className="grid h-20 w-20 place-items-center rounded-full bg-secondary">
          <Scale className="h-8 w-8" />
        </span>
        <h1 className="display mt-8 text-5xl md:text-6xl">Nothing to compare yet</h1>
        <p className="mt-4 max-w-sm text-muted-foreground">Press "Compare" on up to four pieces to see them side by side.</p>
        <Button asChild size="lg" className="group mt-10">
          <Link to="/products">
            Browse the shop
            <ArrowRight className="transition-transform duration-300 group-hover:translate-x-1" />
          </Link>
        </Button>
      </div>
    );
  }

  return (
    <>
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="eyebrow">Side by side</p>
          <h1 className="display mt-3 text-5xl md:text-7xl">Compare</h1>
        </div>
        <Button type="button" variant="outline" onClick={() => dispatch(compareCleared())}>
          Clear all
        </Button>
      </header>
      {products.length === 1 && <p className="mt-4 text-sm text-muted-foreground">Pick another piece to compare it with.</p>}

      <div className="mt-10 overflow-x-auto rounded-2xl border">
        <table className="w-full min-w-xl border-collapse text-sm">
          <caption className="sr-only">The pieces picked for comparison</caption>
          <thead>
            <tr>
              <td className="sticky left-0 z-10 w-28 bg-background" />
              {products.map((product) => (
                <th key={product.id} scope="col" className="min-w-40 p-4 text-left align-top font-normal">
                  <div className="relative">
                    <Link to={`/products/${product.id}`} className="group block">
                      <div className="aspect-5/4 overflow-hidden rounded-xl bg-muted">
                        <ResponsiveImage size="tile" src={product.image} alt="" className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105" />
                      </div>
                      <span className="mt-3 block font-medium leading-snug">{product.title}</span>
                    </Link>
                    <button
                      type="button"
                      aria-label={`Remove ${product.title} from the comparison`}
                      onClick={() => dispatch(removedFromCompare(product.id))}
                      className="absolute right-2 top-2 grid h-8 w-8 place-items-center rounded-full bg-background/90 shadow-sm"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.label} className="border-t">
                <th scope="row" className="sticky left-0 z-10 bg-background p-4 text-left align-top text-[0.7rem] font-medium uppercase tracking-[0.14em] text-muted-foreground">
                  {row.label}
                </th>
                {products.map((product) => (
                  <td key={product.id} className="p-4 align-top tabular-nums">
                    {row.value(product)}
                  </td>
                ))}
              </tr>
            ))}
            <tr className="border-t">
              <td className="sticky left-0 z-10 bg-background" />
              {products.map((product) => (
                <td key={product.id} className="p-4">
                  <Button type="button" size="sm" onClick={(event) => addToBag(product, product.colors[0] ?? '', 1, event.currentTarget).catch(() => undefined)}>
                    <ShoppingBag />
                    Add to bag
                  </Button>
                </td>
              ))}
            </tr>
          </tbody>
        </table>
      </div>
    </>
  );
};

export default Compare;
