import livingRoom from '@/assets/cinematic/look-living-room.mp4';
import livingRoomPoster from '@/assets/cinematic/look-living-room-poster.webp';
import bedroom from '@/assets/cinematic/look-bedroom.mp4';
import bedroomPoster from '@/assets/cinematic/look-bedroom-poster.webp';
import kitchen from '@/assets/cinematic/look-kitchen.mp4';
import kitchenPoster from '@/assets/cinematic/look-kitchen-poster.webp';
import kidsRoom from '@/assets/cinematic/look-kids-room.mp4';
import kidsRoomPoster from '@/assets/cinematic/look-kids-room-poster.webp';
import garden from '@/assets/cinematic/look-garden.mp4';
import gardenPoster from '@/assets/cinematic/look-garden-poster.webp';
import type { ScrollFilmSource } from '@/components/cinematic/ScrollFilm';

/**
 * The clips that walk into a lookbook's room, by the lookbook's slug. Each one ends on that
 * lookbook's own picture (Blobs/Lookbook-*.webp), so a new picture for a look needs a new clip; a look
 * without one simply shows its picture. The living room's (Seedance) has a keyframe every six frames
 * for the home page's scroll; the others (Kling) play once at the top of their own page.
 */
export const lookFilms: Partial<Record<string, ScrollFilmSource>> = {
  'living-room': { video: livingRoom, poster: livingRoomPoster },
  bedroom: { video: bedroom, poster: bedroomPoster },
  kitchen: { video: kitchen, poster: kitchenPoster },
  'kids-room': { video: kidsRoom, poster: kidsRoomPoster },
  // Through the garden doors onto the terrace
  garden: { video: garden, poster: gardenPoster },
};
