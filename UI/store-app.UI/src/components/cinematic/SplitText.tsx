import { Children, cloneElement, createElement, Fragment, isValidElement, type CSSProperties, type ReactElement, type ReactNode } from 'react';
import { useInView } from '@/hooks/use-in-view';
import { motionFlags } from '@/lib/motion';

interface SplitTextProps {
  /** The text; inline elements in it (an <em>) keep wrapping their words. */
  children: ReactNode;
  as?: 'h1' | 'h2' | 'p' | 'blockquote' | 'span';
  className?: string;
  id?: string;
  /** Shows the words now (true) or keeps them down (false); left out, they rise once scrolled into view. */
  shown?: boolean;
  /** Milliseconds between one word and the next. */
  stagger?: number;
  /** Milliseconds before the first word moves. */
  delay?: number;
}

type Wrapper = ReactElement<{ children?: ReactNode }>;

/** The words of the text, each with the inline element around it, if any. */
const wordsOf = (node: ReactNode, wrap?: Wrapper): { word: string; wrap?: Wrapper }[] =>
  Children.toArray(node).flatMap((child) => {
    if (typeof child === 'string' || typeof child === 'number') {
      return String(child)
        .split(/\s+/)
        .filter(Boolean)
        .map((word) => ({ word, wrap }));
    }
    return isValidElement<{ children?: ReactNode }>(child) ? wordsOf(child.props.children, child) : [];
  });

/**
 * A line of text whose words rise one by one from behind their own baseline (styles: split-word in
 * index.css). A screen reader hears the sentence once, whole; the split words are only for the eye.
 */
const SplitText = ({ children, as = 'p', className, id, shown, stagger = 55, delay = 0 }: SplitTextProps) => {
  const [ref, inView] = useInView<HTMLElement>();
  if (!motionFlags.splitHeadlines) return createElement(as, { className, id }, children);

  return createElement(
    as,
    { ref, id, className, 'data-split': (shown ?? inView) ? 'shown' : '' },
    <span className="sr-only">{children}</span>,
    <span aria-hidden>
      {wordsOf(children).map(({ word, wrap }, index) => (
        <Fragment key={index}>
          {index > 0 && ' '}
          <span className="split-word">
            <span style={{ '--split-delay': `${delay + index * stagger}ms` } as CSSProperties}>{wrap ? cloneElement(wrap, {}, word) : word}</span>
          </span>
        </Fragment>
      ))}
    </span>,
  );
};

export default SplitText;
