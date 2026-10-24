import livingRoom from '@/assets/cinematic/look-living-room.mp4';
import livingRoomPoster from '@/assets/cinematic/look-living-room-poster.webp';
import type { ScrollFilmSource } from '@/components/cinematic/ScrollFilm';

/**
 * The clips that walk into a lookbook's room, by the lookbook's slug. Each one ends on that
 * lookbook's own picture (Blobs/Lookbook-*.webp), so a new picture for a look needs a new clip;
 * a look without one simply shows its picture.
 */
export const lookFilms: Partial<Record<string, ScrollFilmSource>> = {
  'living-room': { video: livingRoom, poster: livingRoomPoster },
};
