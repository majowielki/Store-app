import type { SwatchPart } from '@/api/types';

/** Above this relative luminance black stands out from a colour more than white does (WCAG contrast). */
const LIGHT_LUMINANCE = 0.179;

/** Relative luminance of a #rrggbb colour, from 0 (black) to 1 (white). */
const luminance = (hex: string) => {
  const [r, g, b] = [1, 3, 5]
    .map((start) => Number.parseInt(hex.slice(start, start + 2), 16) / 255)
    .map((c) => (c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4));
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
};

/**
 * Whether a mark drawn over the swatch (the tick of the chosen colour) should be dark: the parts'
 * luminance on average, since the mark sits on the split of a two-part swatch. Without parts - a
 * colour the shop does not know - the swatch is taken for light.
 */
export const isLightSwatch = (parts: readonly SwatchPart[] | undefined) =>
  !parts?.length || parts.reduce((sum, part) => sum + luminance(part.color), 0) / parts.length > LIGHT_LUMINANCE;
