// The pages the first screen of the shop needs: the layout, the home page and the error page. Every
// other page is a route module loaded on its first visit (see App.tsx), so it stays out of the
// main bundle.
export { default as HomeLayout } from './HomeLayout';
export { default as Landing } from './Landing';
export { default as Error } from './Error';
