import { useRef, useState, type KeyboardEvent, type PointerEvent } from 'react';
import { X } from 'lucide-react';
import type { Hotspot } from '@/api/types';
import { fieldLabelClass } from '@/components/FormInput';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useProductsBySlug } from '@/hooks/use-products-by-slug';
import { cn } from '@/lib/utils';
import { ProductSearch } from './ProductPicker';

/** Half a percent is finer than anyone can place a point by hand. */
const round = (value: number) => Math.round(Math.min(100, Math.max(0, value)) * 2) / 2;

interface HotspotEditorProps {
  image: string;
  hotspots: Hotspot[];
  onChange: (hotspots: Hotspot[]) => void;
}

/**
 * The points of a lookbook, placed on its picture. A click on the picture adds a point there,
 * dragging a point moves it and the arrow keys move the selected one by 1% (with Shift, 5%).
 * Each point then gets its product from the catalogue search.
 */
const HotspotEditor = ({ image, hotspots, onChange }: HotspotEditorProps) => {
  const pictureRef = useRef<HTMLDivElement>(null);
  const dragging = useRef<number | null>(null);
  const [selected, setSelected] = useState<number | null>(null);
  const { bySlug } = useProductsBySlug(hotspots.map((h) => h.productSlug).filter(Boolean));

  const positionOf = (event: PointerEvent) => {
    const box = pictureRef.current!.getBoundingClientRect();
    return { x: round(((event.clientX - box.left) / box.width) * 100), y: round(((event.clientY - box.top) / box.height) * 100) };
  };
  const update = (index: number, change: Partial<Hotspot>) => onChange(hotspots.map((h, i) => (i === index ? { ...h, ...change } : h)));
  const remove = (index: number) => {
    onChange(hotspots.filter((_, i) => i !== index));
    setSelected(null);
  };

  const addAt = (event: PointerEvent<HTMLDivElement>) => {
    if (event.target !== event.currentTarget && !(event.target instanceof HTMLImageElement)) return;
    onChange([...hotspots, { ...positionOf(event), productSlug: '' }]);
    setSelected(hotspots.length);
  };

  const onKey = (index: number) => (event: KeyboardEvent<HTMLButtonElement>) => {
    const step = event.shiftKey ? 5 : 1;
    const moves: Record<string, [number, number]> = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, -step], ArrowDown: [0, step] };
    const move = moves[event.key];
    if (move) {
      event.preventDefault();
      update(index, { x: round(hotspots[index].x + move[0]), y: round(hotspots[index].y + move[1]) });
    } else if (event.key === 'Delete' || event.key === 'Backspace') {
      event.preventDefault();
      remove(index);
    }
  };

  if (!image) return <p className="text-sm text-muted-foreground">Add the picture first; the points are placed on it.</p>;

  return (
    <div className="grid gap-4">
      <p className={fieldLabelClass}>Points on the picture</p>
      <div
        ref={pictureRef}
        className="relative cursor-crosshair select-none overflow-hidden rounded-xl border"
        onPointerDown={addAt}
        onPointerMove={(event) => dragging.current !== null && update(dragging.current, positionOf(event))}
        onPointerUp={() => (dragging.current = null)}
      >
        <img src={image} alt="" draggable={false} className="block h-auto w-full" />
        {hotspots.map((point, index) => (
          <button
            key={index}
            type="button"
            aria-label={`Point ${index + 1}: ${bySlug.get(point.productSlug)?.title ?? 'no product yet'}, at ${point.x}% and ${point.y}%`}
            aria-pressed={selected === index}
            onPointerDown={(event) => {
              event.stopPropagation();
              event.currentTarget.setPointerCapture(event.pointerId);
              dragging.current = index;
              setSelected(index);
            }}
            onPointerUp={(event) => {
              event.currentTarget.releasePointerCapture(event.pointerId);
              dragging.current = null;
            }}
            onKeyDown={onKey(index)}
            onFocus={() => setSelected(index)}
            className={cn(
              'absolute grid h-7 w-7 -translate-x-1/2 -translate-y-1/2 cursor-grab place-items-center rounded-full border-2 border-white text-xs font-semibold shadow-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
              selected === index ? 'bg-brand text-white' : point.productSlug ? 'bg-white text-foreground' : 'bg-destructive text-white',
            )}
            style={{ left: `${point.x}%`, top: `${point.y}%` }}
          >
            {index + 1}
          </button>
        ))}
      </div>
      <p className="text-xs text-muted-foreground">
        Click the picture to add a point, drag a point to move it. A selected point also moves with the arrow keys (Shift for bigger steps) and
        Delete removes it. A red point has no product yet.
      </p>
      {hotspots.length > 0 && (
        <ol className="grid gap-2">
          {hotspots.map((point, index) => {
            const product = bySlug.get(point.productSlug);
            return (
              <li key={index} className={cn('grid gap-3 rounded-lg border p-3', selected === index && 'border-foreground')}>
                <div className="flex items-center gap-3">
                  <span className="grid h-6 w-6 place-items-center rounded-full bg-muted text-xs font-semibold">{index + 1}</span>
                  <span className="min-w-0 flex-1 truncate text-sm">
                    {product?.title ?? (point.productSlug || 'No product yet')}
                    {point.productSlug && !product && (
                      <Badge variant="outline" className="ml-2">
                        not in the shop
                      </Badge>
                    )}
                  </span>
                  <span className="text-xs tabular-nums text-muted-foreground">
                    {point.x}% · {point.y}%
                  </span>
                  <Button type="button" size="icon" variant="ghost" aria-label={`Remove point ${index + 1}`} onClick={() => remove(index)}>
                    <X />
                  </Button>
                </div>
                {selected === index && (
                  <ProductSearch
                    label={`Product of point ${index + 1}`}
                    exclude={hotspots.map((h) => h.productSlug)}
                    onPick={(slug) => update(index, { productSlug: slug })}
                  />
                )}
              </li>
            );
          })}
        </ol>
      )}
    </div>
  );
};

export default HotspotEditor;
