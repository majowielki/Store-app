/**
 * The smaller copies of the shop's pictures (ADR 016). Next to every picture ".../Name.webp" in
 * the blob container lie ".../w400/Name.webp", w800 and w1200 for srcset, and a 32 px w32 copy
 * drawn behind the picture while it loads. Scripts/make-image-sizes.cs makes them with these widths.
 */
export const IMAGE_WIDTHS = [400, 800, 1200] as const;
export const PLACEHOLDER_WIDTH = 32;
/** The width the shop's pictures are stored at (product shots; the wide editorial ones are larger). */
const ORIGINAL_WIDTH = 1600;

/** How wide a picture is shown, for the browser to pick a copy (the sizes attribute). */
export const imageSizes = {
  /** A small square next to a line: the cart, an order, the search, the lists. */
  thumbnail: '96px',
  /** A tile of the product grids: four across on a desktop, two on a tablet, one on a phone. */
  card: '(min-width: 1024px) 25vw, (min-width: 640px) 50vw, 100vw',
  /** A small tile of a row or a list: recently viewed, the list view of the catalogue. */
  tile: '14rem',
  /** A card of the editorial listings, three across. */
  third: '(min-width: 1280px) 33vw, (min-width: 768px) 50vw, 100vw',
  /** The main picture of a product page or of the quick view. */
  half: '(min-width: 1024px) 55vw, 100vw',
  /** A picture across the page: a hero, a lookbook. */
  full: '100vw',
} as const;

export type ImageSize = keyof typeof imageSizes;

/** The shop's own pictures: an absolute address of a WebP file, no query. Anything else is shown as it is. */
const SIZED_PICTURE = /^(https?:\/\/[^?#]+\/)([^/?#]+\.webp)$/i;

export interface ImageSources {
  src: string;
  srcSet?: string;
  /** The tiny copy to show until the picture has loaded. */
  placeholder?: string;
}

/** The copies of a picture the browser may choose from; none for a picture that has no copies. */
export const imageSources = (url: string): ImageSources => {
  const match = SIZED_PICTURE.exec(url);
  if (!match) return { src: url };
  const [, folder, file] = match;
  const copy = (width: number) => `${folder}w${width}/${file}`;
  return {
    src: url,
    srcSet: [...IMAGE_WIDTHS.map((width) => `${copy(width)} ${width}w`), `${url} ${ORIGINAL_WIDTH}w`].join(', '),
    placeholder: copy(PLACEHOLDER_WIDTH),
  };
};
