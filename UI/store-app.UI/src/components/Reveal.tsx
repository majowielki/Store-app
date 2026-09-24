import { createElement, type CSSProperties, type HTMLAttributes } from 'react';
import { useInView } from '@/hooks/use-in-view';

interface RevealProps extends HTMLAttributes<HTMLElement> {
  as?: 'div' | 'section' | 'li' | 'article' | 'header';
  /** Milliseconds to wait once in view, for staggering siblings. */
  delay?: number;
}

/** Fades and lifts its content in the first time it scrolls into view (styles in index.css). */
const Reveal = ({ as = 'div', delay = 0, style, ...rest }: RevealProps) => {
  const [ref, inView] = useInView<HTMLElement>();
  return createElement(as, {
    ...rest,
    ref,
    'data-reveal': inView ? 'shown' : '',
    style: { '--reveal-delay': `${delay}ms`, ...style } as CSSProperties,
  });
};

export default Reveal;
