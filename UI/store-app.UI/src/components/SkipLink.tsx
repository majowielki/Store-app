import { MAIN_CONTENT_ID } from '@/lib/focus';

/**
 * The first stop of the Tab key on every page, out of sight until then: it jumps past the
 * announcement bar and the header straight to the page's content. The router owns the address,
 * so the link moves the focus itself instead of adding #main-content to it.
 */
const SkipLink = () => {
  const skip = (event: React.MouseEvent<HTMLAnchorElement>) => {
    event.preventDefault();
    const main = document.getElementById(MAIN_CONTENT_ID);
    main?.focus({ preventScroll: true });
    main?.scrollIntoView();
  };

  return (
    <a
      href={`#${MAIN_CONTENT_ID}`}
      onClick={skip}
      className="sr-only rounded-full bg-background text-sm font-medium text-foreground shadow-lg ring-2 ring-ring focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-70 focus:px-5 focus:py-3"
    >
      Skip to content
    </a>
  );
};

export default SkipLink;
