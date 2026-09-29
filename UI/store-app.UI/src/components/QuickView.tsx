import { useRef, useState, type RefObject } from 'react';
import ResponsiveImage from '@/components/ResponsiveImage';
import * as Dialog from '@radix-ui/react-dialog';
import { Link } from 'react-router-dom';
import { ArrowRight, Eye, ShoppingBag, X } from 'lucide-react';
import { useAddToBag } from '@/features/cart/useAddToBag';
import { keepFocusOnNewPage } from '@/lib/focus';
import { cn } from '@/lib/utils';
import type { Product } from '@/utils';
import DeliveryEstimate from './DeliveryEstimate';
import NotifyWhenBack from './NotifyWhenBack';
import { StockNote } from './StockBadge';
import ProductPrice from './ProductPrice';
import SelectProductAmount, { Mode } from './SelectProductAmount';
import SelectProductColor from './SelectProductColor';
import { Button } from './ui/button';

/** The window itself: the picture, the price, a colour, an amount and "Add to bag". */
const QuickViewContent = ({ product, trigger, onAdding }: { product: Product; trigger: RefObject<HTMLButtonElement | null>; onAdding: () => void }) => {
  const [color, setColor] = useState(product.colors[0] ?? '');
  const [amount, setAmount] = useState(1);
  const [adding, setAdding] = useState(false);
  const addToBag = useAddToBag();

  const add = async () => {
    setAdding(true);
    try {
      // The window closes first; the bag slides in and gives the focus back to the card's button
      onAdding();
      await addToBag(product, color, amount, trigger.current);
    } catch {
      // reported by the error middleware
    } finally {
      setAdding(false);
    }
  };

  return (
    <div className="grid gap-6 sm:grid-cols-2">
      <div className="aspect-square overflow-hidden rounded-2xl bg-muted sm:aspect-auto">
        <ResponsiveImage size="half" placeholder src={product.image} alt={product.title} className="h-full w-full object-cover" />
      </div>
      <div className="flex flex-col">
        <p className="eyebrow">{product.company}</p>
        <Dialog.Title className="display mt-2 text-3xl leading-tight">{product.title}</Dialog.Title>
        <p className="mt-3 text-xl">
          <ProductPrice product={product} />
        </p>
        <StockNote product={product} className="mt-2" />
        <Dialog.Description className="mt-4 line-clamp-4 text-sm leading-relaxed text-muted-foreground">{product.description}</Dialog.Description>
        <div className="mt-6 grid gap-6">
          {product.colors.length > 0 && <SelectProductColor colors={product.colors} productColor={color} setProductColor={setColor} />}
          {product.availability === 'outOfStock' ? (
            <NotifyWhenBack productId={product.id} />
          ) : (
            <div className="flex gap-3">
              <SelectProductAmount mode={Mode.SingleProduct} label="Quantity" amount={amount} setAmount={setAmount} max={product.availableQuantity} />
              <Button size="lg" className="h-12 flex-1" onClick={add} disabled={adding}>
                <ShoppingBag />
                Add to bag
              </Button>
            </div>
          )}
        </div>
        <DeliveryEstimate className="mt-4" />
        <Link to={`/products/${product.id}`} className="group mt-auto inline-flex items-center gap-2 pt-6 text-sm font-medium">
          <span className="link-underline">View full details</span>
          <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-1" />
        </Link>
      </div>
    </div>
  );
};

/**
 * "Quick view" on a product card: a window to pick a colour and an amount and put the piece in
 * the bag without leaving the page. The button sits next to the card's link, not inside it.
 */
const QuickView = ({ product, className }: { product: Product; className?: string }) => {
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);

  return (
    <Dialog.Root open={open} onOpenChange={setOpen}>
      <Dialog.Trigger asChild>
        <button
          ref={triggerRef}
          type="button"
          aria-label={`Quick view: ${product.title}`}
          className={cn(
            'inline-flex items-center gap-1.5 rounded-full bg-background/95 px-3.5 py-2 text-xs font-medium shadow-lg backdrop-blur-sm transition-all duration-500 ease-smooth hover:bg-background focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring',
            className,
          )}
        >
          <Eye className="h-3.5 w-3.5" />
          Quick view
        </button>
      </Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/40 backdrop-blur-xs data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0" />
        <Dialog.Content onCloseAutoFocus={keepFocusOnNewPage} className="fixed left-1/2 top-1/2 z-50 max-h-[90vh] w-[calc(100%-2rem)] max-w-3xl -translate-x-1/2 -translate-y-1/2 overflow-y-auto rounded-3xl border bg-background p-5 shadow-2xl data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 sm:p-6">
          <QuickViewContent product={product} trigger={triggerRef} onAdding={() => setOpen(false)} />
          <Dialog.Close className="absolute right-4 top-4 grid h-10 w-10 place-items-center rounded-full bg-background/80 opacity-80 transition-opacity hover:opacity-100 focus:outline-hidden focus-visible:ring-2 focus-visible:ring-ring">
            <X className="h-4 w-4" />
            <span className="sr-only">Close</span>
          </Dialog.Close>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
};

export default QuickView;
