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
} as const;

export type MotionFlag = keyof typeof motionFlags;

/** The width from which the sign-in and registration pages show their picture half (Tailwind's lg). */
export const WIDE_SCREEN_QUERY = '(min-width: 1024px)';

const matches = (query: string) => typeof window !== 'undefined' && typeof window.matchMedia === 'function' && window.matchMedia(query).matches;

/** Whether the visitor asked for less motion; true where it cannot be told (tests), so nothing moves there. */
export const prefersReducedMotion = () =>
  typeof window === 'undefined' || typeof window.matchMedia !== 'function' || matches('(prefers-reduced-motion: reduce)');

/** Whether an effect may run for this visitor: its switch is on and no reduced motion was asked for. */
export const motionAllowed = (flag: MotionFlag) => motionFlags[flag] && !prefersReducedMotion();

/** Whether the screen is at least `query` wide, for effects that only exist on large screens. */
export const screenMatches = matches;

/**
 * Whether the visitor's connection asked to save data (Chrome's Data Saver, a metered network);
 * the films are skipped then.
 */
export const savesData = () =>
  typeof navigator !== 'undefined' && (navigator as Navigator & { connection?: { saveData?: boolean } }).connection?.saveData === true;
