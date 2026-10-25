import oak from '@/assets/cinematic/material-oak.mp4';
import oakPoster from '@/assets/cinematic/material-oak.webp';
import walnut from '@/assets/cinematic/material-walnut.mp4';
import walnutPoster from '@/assets/cinematic/material-walnut.webp';
import linen from '@/assets/cinematic/material-linen.mp4';
import linenPoster from '@/assets/cinematic/material-linen.webp';
import boucle from '@/assets/cinematic/material-boucle.mp4';
import bouclePoster from '@/assets/cinematic/material-boucle.webp';
import travertine from '@/assets/cinematic/material-travertine.mp4';
import travertinePoster from '@/assets/cinematic/material-travertine.webp';
import rattan from '@/assets/cinematic/material-rattan.mp4';
import rattanPoster from '@/assets/cinematic/material-rattan.webp';
import stoneware from '@/assets/cinematic/material-stoneware.mp4';
import stonewarePoster from '@/assets/cinematic/material-stoneware.webp';

export interface Material {
  /** The section's anchor on /materials. */
  key: string;
  name: string;
  /** What the material is and where the shop uses it. */
  about: string;
  /** How to look after it, one habit a line. */
  care: string[];
  /** The catalogue search that finds the pieces made of it (/products?search=...). */
  search: string;
  /** A journal article that says more, if there is one. */
  article?: { slug: string; title: string };
  /** A macro loop of the material with the light moving across it, and its first frame. */
  video: string;
  poster: string;
}

/** The materials the shop's pieces are made of, in the order /materials tells them. */
export const materials: Material[] = [
  {
    key: 'oak',
    name: 'Oak',
    about:
      'Solid and veneered oak carries many of our tables, beds and shelves. It is hard, it takes oil well, and over the years it darkens from straw to honey and wears its marks without fuss.',
    care: [
      'Wipe it with a damp, well-wrung cloth and dry it straight away.',
      'Give oiled oak a fresh coat of oil once or twice a year.',
      'Keep it a step away from radiators: wood moves with the air around it.',
    ],
    search: 'oak',
    article: { slug: 'caring-for-oak', title: 'Caring for oak furniture' },
    video: oak,
    poster: oakPoster,
  },
  {
    key: 'walnut',
    name: 'Walnut',
    about:
      'Darker and finer in the grain than oak, walnut is kept for the pieces meant to be looked at closely - a sideboard front, a bench seat - where its swirling figure has room to show.',
    care: [
      'Dust it with a soft dry cloth; a damp one for marks, dried at once.',
      'Strong sun lightens walnut, so move small things on it now and then.',
      'Use coasters: a wet glass leaves a ring sooner than on oak.',
    ],
    search: 'walnut',
    video: walnut,
    poster: walnutPoster,
  },
  {
    key: 'linen',
    name: 'Linen',
    about:
      'Stonewashed linen dresses our slipcovers, cushions, throws and bedding. It breathes, it softens with every wash, and it creases - which is half of its charm.',
    care: [
      'Wash it at 40 °C with a mild detergent, without fabric softener.',
      'Shake it out and dry it on a line or flat; iron it damp if you must.',
      'Take removable covers off before they need it, not after.',
    ],
    search: 'linen',
    video: linen,
    poster: linenPoster,
  },
  {
    key: 'boucle',
    name: 'Bouclé',
    about:
      'A yarn full of small loops, woven into a nubbly cloth that hides everyday wear and catches the light. We use it on sofas and chairs that are meant to be sat in.',
    care: [
      'Vacuum it now and then with a soft brush head.',
      'Blot a spill with a clean cloth; never rub it into the loops.',
      'Trim a snagged loop with scissors - never pull it.',
    ],
    search: 'boucle',
    video: boucle,
    poster: bouclePoster,
  },
  {
    key: 'travertine',
    name: 'Travertine',
    about:
      'A limestone full of small pores and soft banding, honed to a matt finish for coffee tables, side tables and lamp bases. No two pieces have the same pattern.',
    care: [
      'Put coasters under glasses: wine, citrus and vinegar etch the stone.',
      'Wipe it with water and a pH-neutral cleaner only.',
      'Reseal the surface about once a year.',
    ],
    search: 'travertine',
    video: travertine,
    poster: travertinePoster,
  },
  {
    key: 'rattan',
    name: 'Rattan',
    about:
      'Woven palm cane: light, a little springy, cool to sit on in summer. It turns from pale gold to a deeper honey as it ages.',
    care: [
      'Dust the weave with a soft brush.',
      'Wipe it with a damp cloth now and then - rattan likes a little moisture.',
      'Keep it out of the rain and away from a heater.',
    ],
    search: 'rattan',
    video: rattan,
    poster: rattanPoster,
  },
  {
    key: 'stoneware',
    name: 'Stoneware',
    about:
      'Clay fired hot enough to become dense and strong, for vases, lamps and tableware. The small dark speckles are iron in the clay coming through the glaze.',
    care: [
      'Wash it by hand to keep the glaze at its best.',
      'Spare it sudden changes of temperature.',
      'A vase holds water; a lamp base wants only a dry cloth.',
    ],
    search: 'stoneware',
    video: stoneware,
    poster: stonewarePoster,
  },
];

const plain = (text: string) =>
  text
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase();

/** The materials of the guide that a product's materials list names ("Solid Oak", "Bouclé fabric"). */
export const materialsIn = (names: readonly string[]): Material[] => {
  const text = plain(names.join(' '));
  return materials.filter((material) => text.includes(plain(material.search)));
};
