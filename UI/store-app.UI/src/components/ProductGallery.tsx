import { useRef, useState, type KeyboardEvent, type PointerEvent } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { ChevronLeft, ChevronRight, Expand, X } from 'lucide-react';
import type { ProductDetail } from '@/api/types';
import LookbookPicture from '@/components/content/LookbookPicture';
import { useProductsBySlug } from '@/hooks/use-products-by-slug';
import { cn } from '@/lib/utils';

interface Picture {
  url: string;
  alt: string;
}

const prefersReducedMotion = () => window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;

const roundButton =
  'grid h-10 w-10 place-items-center rounded-full bg-background/90 text-foreground shadow-lg backdrop-blur-sm transition-opacity hover:bg-background focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring';

/** A picture magnified under the mouse; a finger scrolls the gallery instead. */
const ZoomPicture = ({ picture }: { picture: Picture }) => {
  const [zoomed, setZoomed] = useState(false);
  const [origin, setOrigin] = useState('50% 50%');
  const follow = (event: PointerEvent<HTMLDivElement>) => {
    const box = event.currentTarget.getBoundingClientRect();
    setOrigin(`${((event.clientX - box.left) / box.width) * 100}% ${((event.clientY - box.top) / box.height) * 100}%`);
  };
  return (
    <div
      className="h-full w-full cursor-zoom-in overflow-hidden"
      onPointerEnter={(event) => event.pointerType === 'mouse' && setZoomed(true)}
      onPointerLeave={() => setZoomed(false)}
      onPointerMove={(event) => event.pointerType === 'mouse' && follow(event)}
    >
      <img
        src={picture.url}
        alt={picture.alt}
        loading="lazy"
        draggable={false}
        className="h-full w-full object-cover transition-transform duration-500 ease-smooth"
        style={{ transformOrigin: origin, transform: zoomed ? 'scale(1.8)' : 'scale(1)' }}
      />
    </div>
  );
};

/**
 * The pictures of a product: the room picture first, with a point on every other product of the
 * shop it shows, then the gallery. Swipe, the arrows beside the picture, the thumbnails or the
 * arrow keys move between them; "View larger" opens the one in view over the page.
 */
const ProductGallery = ({ product }: { product: ProductDetail }) => {
  const pictures: Picture[] = [{ url: product.image, alt: product.title }, ...product.images];
  const count = pictures.length;
  const slugs = product.hotspots.map((point) => point.productSlug);
  // A point to a product the shop does not list (retired, or not added yet) is not drawn
  const { bySlug } = useProductsBySlug(slugs.length > 0 ? slugs : undefined);
  const [current, setCurrent] = useState(0);
  const [enlarged, setEnlarged] = useState(false);
  const trackRef = useRef<HTMLDivElement>(null);
  // While the gallery scrolls to a picture, the pictures it passes do not become the current one
  const scrollingTo = useRef<number | null>(null);

  const goTo = (index: number) => {
    const next = (index + count) % count;
    setCurrent(next);
    const track = trackRef.current;
    if (!track || track.clientWidth === 0) return;
    scrollingTo.current = next;
    track.scrollTo?.({ left: next * track.clientWidth, behavior: prefersReducedMotion() ? 'auto' : 'smooth' });
  };

  const onScroll = () => {
    const track = trackRef.current;
    if (!track || track.clientWidth === 0) return;
    const index = Math.round(track.scrollLeft / track.clientWidth);
    if (scrollingTo.current !== null) {
      if (index === scrollingTo.current) scrollingTo.current = null;
      return;
    }
    setCurrent(index);
  };

  const onKeyDown = (event: KeyboardEvent) => {
    if (count < 2) return;
    if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
      event.preventDefault();
      goTo(current + (event.key === 'ArrowLeft' ? -1 : 1));
    }
  };

  const arrows = (placement: string) =>
    count > 1 && (
      <>
        <button type="button" aria-label="Previous picture" onClick={() => goTo(current - 1)} className={cn(roundButton, 'absolute left-4 top-1/2 -translate-y-1/2', placement)}>
          <ChevronLeft className="h-5 w-5" />
        </button>
        <button type="button" aria-label="Next picture" onClick={() => goTo(current + 1)} className={cn(roundButton, 'absolute right-4 top-1/2 -translate-y-1/2', placement)}>
          <ChevronRight className="h-5 w-5" />
        </button>
      </>
    );

  return (
    <div className="grid gap-3">
      <div role="region" aria-roledescription="carousel" aria-label={`Pictures of the ${product.title}`} onKeyDown={onKeyDown} className="group/gallery relative">
        <div
          ref={trackRef}
          onScroll={onScroll}
          onPointerDown={() => (scrollingTo.current = null)}
          onWheel={() => (scrollingTo.current = null)}
          className="flex aspect-4/3 snap-x snap-mandatory overflow-x-auto overflow-y-hidden rounded-4xl bg-muted scrollbar-none [&::-webkit-scrollbar]:hidden"
        >
          {pictures.map((picture, index) => (
            <div
              key={`${index}-${picture.url}`}
              // Only the picture in view takes part in the tab order
              inert={index !== current}
              role="group"
              aria-roledescription="slide"
              aria-label={`${index + 1} of ${count}`}
              className="relative h-full w-full shrink-0 snap-center"
            >
              {index === 0 ? (
                <LookbookPicture
                  image={picture.url}
                  alt={picture.alt}
                  hotspots={product.hotspots}
                  products={bySlug}
                  priority
                  viewTransition="product-image"
                  className="h-full rounded-none bg-transparent"
                  imageClassName="h-full object-cover"
                />
              ) : (
                <ZoomPicture picture={picture} />
              )}
            </div>
          ))}
        </div>
        {arrows('opacity-0 group-hover/gallery:opacity-100 focus-visible:opacity-100 max-md:hidden')}
        <button type="button" aria-label="View larger" onClick={() => setEnlarged(true)} className={cn(roundButton, 'absolute right-4 top-4')}>
          <Expand className="h-4 w-4" />
        </button>
      </div>

      {count > 1 && (
        <div role="group" aria-label="Choose a picture" className="flex gap-3" onKeyDown={onKeyDown}>
          {pictures.map((picture, index) => (
            <button
              key={`${index}-${picture.url}`}
              type="button"
              aria-label={`Show picture ${index + 1} of ${count}`}
              aria-current={index === current ? 'true' : undefined}
              onClick={() => goTo(index)}
              className={cn(
                'aspect-4/3 w-20 overflow-hidden rounded-xl border-2 transition focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring sm:w-24',
                index === current ? 'border-foreground' : 'border-transparent opacity-70 hover:opacity-100',
              )}
            >
              <img src={picture.url} alt="" loading="lazy" className="h-full w-full object-cover" />
            </button>
          ))}
        </div>
      )}

      <Dialog.Root open={enlarged} onOpenChange={setEnlarged}>
        <Dialog.Portal>
          <Dialog.Overlay className="fixed inset-0 z-50 bg-black/85 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0" />
          <Dialog.Content
            aria-describedby={undefined}
            onKeyDown={onKeyDown}
            className="fixed inset-0 z-50 grid place-items-center p-4 focus:outline-hidden data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 sm:p-10"
          >
            <Dialog.Title className="sr-only">{`${product.title}, picture ${current + 1} of ${count}`}</Dialog.Title>
            <img src={pictures[current].url} alt={pictures[current].alt} className="max-h-full max-w-full rounded-2xl object-contain" />
            {arrows('')}
            <Dialog.Close aria-label="Close" className={cn(roundButton, 'absolute right-4 top-4')}>
              <X className="h-4 w-4" />
            </Dialog.Close>
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>
    </div>
  );
};

export default ProductGallery;
