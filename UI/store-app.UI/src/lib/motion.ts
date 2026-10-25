/**
 * The shop's cinematic effects, each behind its own switch: set one to false and its place shows the
 * still picture or the plain text again. Visitors who asked their system for less motion get the still
 * version of all of them, whatever the switches say.
 */
export const motionFlags = {
  /** The short film on the sign-in and registration pages before their picture (AuthShell). */
  authFilm: true,
  /** Headlines whose words rise one by one into place (SplitText). */
  splitHeadlines: true,
  /** Smooth scrolling of the shop's pages (Lenis). */
  smoothScroll: true,
  /** The morning light moving across the home page's hero picture. */
  heroLight: true,
  /** The walk into the featured lookbook's room on the home page, following the scroll (ScrollFilm). */
  lookbookEntrance: true,
  /** The empty room with the curtain moving in the light, behind the 404 page. */
  emptyRoom: true,
  /** The home page hero at sunset (its own picture and loop) when the shop is shown in the dark theme. */
  eveningHero: true,
  /** The light moving in the covers of the collections and the makers' workshops. */
  coverLoops: true,
  /** The close looks at the materials on /materials, with the light moving across them. */
  materialLoops: true,
  /** The morning light reaching the linen-wrapped parcel when an order is paid. */
  orderParcel: true,
  /** The walk from the oak forest through the workshop into a room on the About page, following the scroll. */
  aboutJourney: true,
} as const;

export type MotionFlag = keyof typeof motionFlags;

/** The media query of visitors who asked their system for less motion. */
export const REDUCED_MOTION_QUERY = '(prefers-reduced-motion: reduce)';

/** A mouse or trackpad: touch screens keep their own native scrolling. */
export const FINE_POINTER_QUERY = '(pointer: fine)';

/** The width from which the sign-in and registration pages show their picture half (Tailwind's lg). */
export const WIDE_SCREEN_QUERY = '(min-width: 1024px)';

const matches = (query: string) => typeof window !== 'undefined' && typeof window.matchMedia === 'function' && window.matchMedia(query).matches;

/** Whether the visitor asked for less motion; true where it cannot be told (tests), so nothing moves there. */
export const prefersReducedMotion = () =>
  typeof window === 'undefined' || typeof window.matchMedia !== 'function' || matches(REDUCED_MOTION_QUERY);

/** Whether an effect may run for this visitor: its switch is on and no reduced motion was asked for. */
export const motionAllowed = (flag: MotionFlag) => motionFlags[flag] && !prefersReducedMotion();

/** Whether a media query matches (false where it cannot be told), e.g. for effects only large screens have. */
export const screenMatches = matches;

/**
 * Whether the visitor's connection asked to save data (Chrome's Data Saver, a metered network);
 * the films are skipped then.
 */
export const savesData = () =>
  typeof navigator !== 'undefined' && (navigator as Navigator & { connection?: { saveData?: boolean } }).connection?.saveData === true;
