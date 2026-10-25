import warmMinimal from '@/assets/cinematic/collection-warm-minimal.mp4';
import warmMinimalPoster from '@/assets/cinematic/collection-warm-minimal.webp';
import smallSpaces from '@/assets/cinematic/collection-small-spaces.mp4';
import smallSpacesPoster from '@/assets/cinematic/collection-small-spaces.webp';
import outdoorSeason from '@/assets/cinematic/collection-outdoor-season.mp4';
import outdoorSeasonPoster from '@/assets/cinematic/collection-outdoor-season.webp';
import artifex from '@/assets/cinematic/maker-artifex.mp4';
import artifexPoster from '@/assets/cinematic/maker-artifex.webp';
import comfora from '@/assets/cinematic/maker-comfora.mp4';
import comforaPoster from '@/assets/cinematic/maker-comfora.webp';
import homestead from '@/assets/cinematic/maker-homestead.mp4';
import homesteadPoster from '@/assets/cinematic/maker-homestead.webp';
import luxora from '@/assets/cinematic/maker-luxora.mp4';
import luxoraPoster from '@/assets/cinematic/maker-luxora.webp';
import modenza from '@/assets/cinematic/maker-modenza.mp4';
import modenzaPoster from '@/assets/cinematic/maker-modenza.webp';

/** A cover's loop: the cover picture with only its light moving (start and end frame are the picture). */
export interface CoverFilm {
  video: string;
  /** The loop's first frame, cropped like the loop, standing in for the cover picture. */
  poster: string;
}

/**
 * The loops of the editorial covers, by slug. Each starts and ends on its cover in Blobs/
 * (Collection-*.webp, Maker-*.webp), so a new cover needs a new loop; a page without one shows its
 * cover as before. Each collection keeps its own time of day: Warm Minimal the early morning, Small
 * Spaces midday, Outdoor Season the sunset; the makers' workshops have dust drifting in the light.
 */
export const collectionFilms: Partial<Record<string, CoverFilm>> = {
  'warm-minimal': { video: warmMinimal, poster: warmMinimalPoster },
  'small-spaces': { video: smallSpaces, poster: smallSpacesPoster },
  'outdoor-season': { video: outdoorSeason, poster: outdoorSeasonPoster },
};

export const makerFilms: Partial<Record<string, CoverFilm>> = {
  artifex: { video: artifex, poster: artifexPoster },
  comfora: { video: comfora, poster: comforaPoster },
  homestead: { video: homestead, poster: homesteadPoster },
  luxora: { video: luxora, poster: luxoraPoster },
  modenza: { video: modenza, poster: modenzaPoster },
};
