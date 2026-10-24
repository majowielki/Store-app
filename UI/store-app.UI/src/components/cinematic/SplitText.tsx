import { createElement, Fragment, type CSSProperties } from 'react';
import { useInView } from '@/hooks/use-in-view';
import { cn } from '@/lib/utils';
import { motionFlags } from '@/lib/motion';

interface SplitTextProps {
  text: string;
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

/**
 * A line of text whose words rise one by one from behind their own baseline (styles: split-word in
 * index.css). A screen reader hears the sentence once, whole; the split words are only for the eye.
 */
const SplitText = ({ text, as = 'p', className, id, shown, stagger = 55, delay = 0 }: SplitTextProps) => {
  const [ref, inView] = useInView<HTMLElement>();
  if (!motionFlags.splitHeadlines) return createElement(as, { className, id }, text);

  const words = text.split(/\s+/).filter(Boolean);
  return createElement(
    as,
    { ref, id, className: cn(className), 'data-split': (shown ?? inView) ? 'shown' : '' },
    <span className="sr-only">{text}</span>,
    <span aria-hidden>
      {words.map((word, index) => (
        <Fragment key={index}>
          {index > 0 && ' '}
          <span className="split-word">
            <span style={{ '--split-delay': `${delay + index * stagger}ms` } as CSSProperties}>{word}</span>
          </span>
        </Fragment>
      ))}
    </span>,
  );
};

export default SplitText;
