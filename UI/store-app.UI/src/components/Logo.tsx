import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';

/** The wordmark: the shop's name in the display serif, with the terracotta full stop. */
const Logo = ({ className, onClick }: { className?: string; onClick?: () => void }) => (
  <Link
    to="/"
    onClick={onClick}
    aria-label="Store, home page"
    className={cn('display text-[1.75rem] leading-none transition-opacity hover:opacity-70', className)}
  >
    store<span className="text-brand">.</span>
  </Link>
);
export default Logo;
