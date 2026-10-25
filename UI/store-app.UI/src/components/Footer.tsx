import { useId, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowRight, ArrowUp, Plus } from 'lucide-react';
import { infoPages, inspirationPages } from '@/content/pages';
import { cn } from '@/lib/utils';
import { categories, categoryHref } from '@/utils/categories';
import { Button } from './ui/button';

const columns = [
  {
    title: 'Shop',
    links: [
      { to: '/products', label: 'All products' },
      ...categories.slice(0, 4).map((c) => ({ to: categoryHref(c), label: c.label })),
      { to: '/products?sale=on', label: 'Sale' },
    ],
  },
  { title: 'Inspiration', links: inspirationPages },
  {
    title: 'Store',
    links: [
      { to: '/about', label: 'About us' },
      { to: '/contact', label: 'Contact' },
      { to: '/orders', label: 'Your orders' },
      { to: '/wishlist', label: 'Wishlist' },
      { to: '/cart', label: 'Cart' },
    ],
  },
  { title: 'Help', links: infoPages },
];

type Column = (typeof columns)[number];

/**
 * A column of the site map. On a phone the columns fold (the footer would otherwise be taller than
 * the screen and outweigh a short page): the title is a button that opens its links. From sm up
 * every column stands open and the title is a plain heading again.
 */
const FooterColumn = ({ column }: { column: Column }) => {
  const [open, setOpen] = useState(false);
  const listId = useId();
  const titleClass = 'eyebrow text-primary-foreground/50 dark:text-muted-foreground';
  return (
    <div className="border-b border-primary-foreground/15 dark:border-border sm:border-0">
      <h2 className={cn(titleClass, 'hidden sm:block')}>{column.title}</h2>
      <button
        type="button"
        aria-expanded={open}
        aria-controls={listId}
        onClick={() => setOpen((value) => !value)}
        className={cn(titleClass, 'flex w-full items-center justify-between py-4 sm:hidden')}
      >
        {column.title}
        <Plus className={cn('h-4 w-4 transition-transform duration-300', open && 'rotate-45')} aria-hidden />
      </button>
      <ul id={listId} className={cn('space-y-3 pb-5 text-sm sm:mt-5 sm:block sm:pb-0', !open && 'hidden')}>
        {column.links.map((link) => (
          <li key={link.label}>
            <Link to={link.to} className="link-underline">
              {link.label}
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
};

/** An inverted footer: a last call to shop, the site map, the address and an oversized wordmark. */
const Footer = () => (
  <footer className="mt-24 bg-primary pb-28 text-primary-foreground dark:border-t dark:bg-card dark:text-card-foreground md:pb-0">
    <div className="align-element grid gap-10 pt-14 sm:gap-14 sm:pt-20 lg:grid-cols-12">
      <div className="lg:col-span-5">
        <p className="display text-4xl leading-[1.05] md:text-6xl">
          Make yourself <em className="text-brand">at home.</em>
        </p>
        <p className="mt-5 max-w-sm text-sm text-primary-foreground/60 dark:text-muted-foreground">
          Furniture and objects for every room, chosen to be lived with for years — delivered to your door.
        </p>
        <Button asChild variant="brand" size="lg" className="group mt-8">
          <Link to="/products">
            Shop the collection
            <ArrowRight className="transition-transform duration-300 group-hover:translate-x-1" />
          </Link>
        </Button>
      </div>

      <div className="grid sm:grid-cols-3 sm:gap-10 lg:col-span-7">
        {columns.map((column) => (
          <FooterColumn key={column.title} column={column} />
        ))}
        <div className="mt-8 sm:mt-0">
          <h2 className="eyebrow text-primary-foreground/50 dark:text-muted-foreground">Visit</h2>
          <address className="mt-5 space-y-3 text-sm not-italic text-primary-foreground/60 dark:text-muted-foreground">
            <p>
              Strzegomska 140A
              <br />
              54-429 Wrocław
            </p>
            <p>+48 000 000 000</p>
            <p>
              <a href="mailto:contact@store.com" className="link-underline text-primary-foreground dark:text-foreground">
                contact@store.com
              </a>
            </p>
          </address>
        </div>
      </div>
    </div>

    <div className="align-element mt-16 flex items-center justify-between gap-4 border-t border-primary-foreground/15 py-6 text-xs text-primary-foreground/50 dark:border-border dark:text-muted-foreground">
      <span>© {new Date().getFullYear()} Store. All rights reserved.</span>
      <button
        type="button"
        onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
        className="group inline-flex items-center gap-2 transition-colors hover:text-primary-foreground dark:hover:text-foreground"
      >
        Back to top
        <ArrowUp className="h-3.5 w-3.5 transition-transform duration-300 group-hover:-translate-y-0.5" />
      </button>
    </div>

    {/* The wordmark is a texture, not text: drawn by CSS, so neither a screen reader nor a contrast check reads it */}
    <div aria-hidden className="overflow-hidden">
      <div
        data-wordmark="store."
        className="display select-none text-center text-[27vw] leading-[0.72] tracking-tighter text-primary-foreground/[0.07] before:content-[attr(data-wordmark)] dark:text-foreground/6"
      />
    </div>
  </footer>
);

export default Footer;
