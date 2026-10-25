import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { ArrowRight } from 'lucide-react';
import { Button } from './ui/button';

interface EmptyStateProps {
  /** A quiet, empty space waiting to be filled (no products: nothing the shop does not sell). */
  image: string;
  title: string;
  text: string;
  action: { to: string; label: string };
  children?: ReactNode;
}

/** An empty cart, wishlist or comparison: one calm picture, what to do next and the way to do it. */
const EmptyState = ({ image, title, text, action, children }: EmptyStateProps) => (
  <div className="grid place-items-center py-12 text-center md:py-20">
    <div className="w-full max-w-md animate-fade-up overflow-hidden rounded-4xl bg-muted">
      <img src={image} alt="" width={960} height={640} className="aspect-3/2 w-full animate-zoom-out object-cover" />
    </div>
    <h1 className="display mt-10 animate-fade-up text-5xl [animation-delay:80ms] md:text-6xl">{title}</h1>
    <p className="mt-4 max-w-sm animate-fade-up text-muted-foreground [animation-delay:160ms]">{text}</p>
    <Button asChild size="lg" className="group mt-10 animate-fade-up [animation-delay:240ms]">
      <Link to={action.to}>
        {action.label}
        <ArrowRight className="transition-transform duration-300 group-hover:translate-x-1" />
      </Link>
    </Button>
    {children}
  </div>
);

export default EmptyState;
