import { shopUrl } from '@/config';

// The shop as search engines and social sites describe it when a page says nothing of its own.
// index.html carries the same values for the crawlers that do not run the app.

export const SITE_NAME = 'Store';
/** The home page's title, and the page title of anything without its own. */
export const SITE_TITLE = 'Store — furniture & home';
export const SITE_DESCRIPTION = 'Furniture and home goods for every room, delivered to your door.';
/** The picture social sites show for a page without one of its own (public/og-image.jpg). */
export const SITE_IMAGE = '/og-image.jpg';
export const SITE_IMAGE_ALT = 'A cream sofa in a sunlit living room, with the words Store feels like home';
/** The logo in the organisation's structured data. */
export const SITE_LOGO = '/favicon.svg';

/** "Oak Desk — Store". */
export const pageTitle = (title?: string) => (title ? `${title} — ${SITE_NAME}` : SITE_TITLE);

/** An address of the shop, absolute (search engines and social sites need that) under its public address. */
export const absoluteUrl = (path: string) => new URL(path, `${shopUrl}/`).toString();
