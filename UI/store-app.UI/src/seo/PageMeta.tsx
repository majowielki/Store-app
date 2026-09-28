import { usePageMeta, type PageMetaProps } from './usePageMeta';

/**
 * A page's title, description and social card (see usePageMeta), as an element: a page renders
 * it in the branch that has the data - a product once it is loaded, "not found" when it is not.
 * One per page; the shop's own values come back when it goes.
 */
const PageMeta = (props: PageMetaProps) => {
  usePageMeta(props);
  return null;
};

export default PageMeta;
