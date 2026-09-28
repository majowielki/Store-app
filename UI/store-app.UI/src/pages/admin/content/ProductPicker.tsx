import { useEffect, useId, useState } from 'react';
import ResponsiveImage from '@/components/ResponsiveImage';
import { ArrowDown, ArrowUp, Plus, X } from 'lucide-react';
import { useGetProductsQuery } from '@/api/catalog';
import { fieldLabelClass } from '@/components/FormInput';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useProductsBySlug } from '@/hooks/use-products-by-slug';

/** The catalogue search the pickers share: products matching the text, once it has two letters. */
const useProductSearch = (text: string) => {
  const [query, setQuery] = useState(text);
  useEffect(() => {
    const timer = window.setTimeout(() => setQuery(text.trim()), 250);
    return () => window.clearTimeout(timer);
  }, [text]);
  const { data, isFetching } = useGetProductsQuery({ search: query, pageSize: 8 }, { skip: query.length < 2 });
  return { results: query.length < 2 ? [] : (data?.items ?? []), isFetching };
};

interface ProductSearchProps {
  label: string;
  /** Slugs already chosen; they are not offered again. */
  exclude: string[];
  onPick: (slug: string) => void;
}

/** A search box over the catalogue with the matches as buttons. */
export const ProductSearch = ({ label, exclude, onPick }: ProductSearchProps) => {
  const id = useId();
  const [text, setText] = useState('');
  const { results, isFetching } = useProductSearch(text);
  const offered = results.filter((product) => !exclude.includes(product.slug));

  return (
    <div className="grid gap-2">
      <Label htmlFor={id} className={fieldLabelClass}>
        {label}
      </Label>
      <Input id={id} type="search" value={text} onChange={(e) => setText(e.target.value)} placeholder="Search the catalogue: sofa, oak, lamp…" />
      {text.trim().length >= 2 && (
        <ul className="max-h-64 overflow-y-auto rounded-lg border" aria-busy={isFetching}>
          {offered.length === 0 && <li className="p-3 text-sm text-muted-foreground">{isFetching ? 'Searching…' : 'No product matches.'}</li>}
          {offered.map((product) => (
            <li key={product.id} className="flex items-center gap-3 border-b p-2 last:border-b-0">
              <ResponsiveImage size="thumbnail" src={product.image} alt="" className="h-10 w-12 rounded object-cover" />
              <span className="min-w-0 flex-1 truncate text-sm">{product.title}</span>
              <Button
                type="button"
                size="sm"
                variant="outline"
                aria-label={`Add ${product.title}`}
                onClick={() => {
                  onPick(product.slug);
                  setText('');
                }}
              >
                <Plus />
              </Button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

/**
 * The products of a collection or an article, by slug, in the order the shop shows them: found
 * with the search, moved up and down, removed. A slug the catalogue no longer lists is kept but
 * marked, since the shop leaves it out.
 */
const ProductPicker = ({ value, onChange }: { value: string[]; onChange: (slugs: string[]) => void }) => {
  const { bySlug } = useProductsBySlug(value);
  const move = (index: number, by: number) => {
    const next = [...value];
    [next[index], next[index + by]] = [next[index + by], next[index]];
    onChange(next);
  };

  return (
    <div className="grid gap-4">
      <ProductSearch label="Products" exclude={value} onPick={(slug) => onChange([...value, slug])} />
      {value.length > 0 && (
        <ol className="grid gap-2">
          {value.map((slug, index) => {
            const product = bySlug.get(slug);
            return (
              <li key={slug} className="flex items-center gap-3 rounded-lg border p-2">
                <span className="w-5 text-center text-xs text-muted-foreground">{index + 1}</span>
                {product ? <ResponsiveImage size="thumbnail" src={product.image} alt="" className="h-10 w-12 rounded object-cover" /> : <span className="h-10 w-12 rounded bg-muted" />}
                <span className="min-w-0 flex-1 truncate text-sm">
                  {product?.title ?? slug}
                  {!product && (
                    <Badge variant="outline" className="ml-2">
                      not in the shop
                    </Badge>
                  )}
                </span>
                <Button type="button" size="icon" variant="ghost" aria-label={`Move ${slug} up`} disabled={index === 0} onClick={() => move(index, -1)}>
                  <ArrowUp />
                </Button>
                <Button type="button" size="icon" variant="ghost" aria-label={`Move ${slug} down`} disabled={index === value.length - 1} onClick={() => move(index, 1)}>
                  <ArrowDown />
                </Button>
                <Button type="button" size="icon" variant="ghost" aria-label={`Remove ${slug}`} onClick={() => onChange(value.filter((s) => s !== slug))}>
                  <X />
                </Button>
              </li>
            );
          })}
        </ol>
      )}
    </div>
  );
};

export default ProductPicker;
