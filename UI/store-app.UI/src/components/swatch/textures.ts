import type { SwatchTexture } from '@/api/types';
import acacia from '@/assets/swatches/acacia.webp';
import bamboo from '@/assets/swatches/bamboo.webp';
import beech from '@/assets/swatches/beech.webp';
import birch from '@/assets/swatches/birch.webp';
import blackSteel from '@/assets/swatches/black-steel.webp';
import brass from '@/assets/swatches/brass.webp';
import cane from '@/assets/swatches/cane.webp';
import greyRattan from '@/assets/swatches/grey-rattan.webp';
import greyStoneware from '@/assets/swatches/grey-stoneware.webp';
import honeyOak from '@/assets/swatches/honey-oak.webp';
import jute from '@/assets/swatches/jute.webp';
import lightOak from '@/assets/swatches/light-oak.webp';
import marble from '@/assets/swatches/marble.webp';
import naturalOak from '@/assets/swatches/natural-oak.webp';
import oliveWood from '@/assets/swatches/olive-wood.webp';
import pine from '@/assets/swatches/pine.webp';
import rattanWeave from '@/assets/swatches/rattan-weave.webp';
import rattan from '@/assets/swatches/rattan.webp';
import reclaimedWood from '@/assets/swatches/reclaimed-wood.webp';
import rubberwood from '@/assets/swatches/rubberwood.webp';
import seagrass from '@/assets/swatches/seagrass.webp';
import speckledStoneware from '@/assets/swatches/speckled-stoneware.webp';
import stone from '@/assets/swatches/stone.webp';
import teak from '@/assets/swatches/teak.webp';
import terracotta from '@/assets/swatches/terracotta.webp';
import travertine from '@/assets/swatches/travertine.webp';
import walnut from '@/assets/swatches/walnut.webp';
import whiteGlaze from '@/assets/swatches/white-glaze.webp';

/**
 * The pictures of the woods, weaves, stones and metals a swatch shows: crops of the product
 * photographs (Scripts/swatches.json, made by Scripts/make-swatches.cs). Typed by the API's list,
 * so a surface the catalogue adds cannot go without its picture.
 */
export const swatchTextures: Record<SwatchTexture, string> = {
  acacia,
  bamboo,
  beech,
  birch,
  blackSteel,
  brass,
  cane,
  greyRattan,
  greyStoneware,
  honeyOak,
  jute,
  lightOak,
  marble,
  naturalOak,
  oliveWood,
  pine,
  rattanWeave,
  rattan,
  reclaimedWood,
  rubberwood,
  seagrass,
  speckledStoneware,
  stone,
  teak,
  terracotta,
  travertine,
  walnut,
  whiteGlaze,
};
