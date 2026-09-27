import { ArrowDown, ArrowUp, ImagePlus, X } from 'lucide-react';
import type { ProductImage } from '@/api/types';
import { fieldLabelClass } from '@/components/FormInput';
import { Button } from '@/components/ui/button';
import { TextField } from './content/fields';

/** The most pictures a product has besides the main one (the API's limit). */
export const maxGalleryPictures = 12;

interface GalleryEditorProps {
  images: ProductImage[];
  onChange: (images: ProductImage[]) => void;
}

/**
 * The pictures shown after the main one, in order: each with its address and a short description
 * for screen readers. A product without any keeps its main picture only.
 */
const GalleryEditor = ({ images, onChange }: GalleryEditorProps) => {
  const update = (index: number, change: Partial<ProductImage>) => onChange(images.map((image, i) => (i === index ? { ...image, ...change } : image)));
  const move = (index: number, by: number) => {
    const next = [...images];
    [next[index], next[index + by]] = [next[index + by], next[index]];
    onChange(next);
  };

  return (
    <div className="grid gap-4">
      <p className={fieldLabelClass}>Gallery</p>
      {images.length === 0 && <p className="text-sm text-muted-foreground">No pictures besides the main one; the product page shows that one only.</p>}
      <ol className="grid gap-3">
        {images.map((image, index) => {
          const number = index + 2;
          return (
            <li key={index} className="grid gap-3 rounded-lg border p-3 sm:grid-cols-[5rem_1fr_auto] sm:items-start">
              <div className="aspect-[4/3] w-20 overflow-hidden rounded-md bg-muted">{image.url && <img src={image.url} alt="" className="h-full w-full object-cover" />}</div>
              <div className="grid gap-3">
                <TextField label={`Picture ${number}: address`} type="url" value={image.url} onChange={(url) => update(index, { url })} required />
                <TextField label={`Picture ${number}: what it shows`} value={image.alt} onChange={(alt) => update(index, { alt })} required maxLength={200} />
              </div>
              <div className="flex gap-1 sm:flex-col">
                <Button type="button" size="icon" variant="ghost" aria-label={`Move picture ${number} up`} disabled={index === 0} onClick={() => move(index, -1)}>
                  <ArrowUp />
                </Button>
                <Button type="button" size="icon" variant="ghost" aria-label={`Move picture ${number} down`} disabled={index === images.length - 1} onClick={() => move(index, 1)}>
                  <ArrowDown />
                </Button>
                <Button type="button" size="icon" variant="ghost" aria-label={`Remove picture ${number}`} onClick={() => onChange(images.filter((_, i) => i !== index))}>
                  <X />
                </Button>
              </div>
            </li>
          );
        })}
      </ol>
      <Button type="button" variant="outline" className="justify-self-start" disabled={images.length >= maxGalleryPictures} onClick={() => onChange([...images, { url: '', alt: '' }])}>
        <ImagePlus />
        Add a picture
      </Button>
    </div>
  );
};

export default GalleryEditor;
