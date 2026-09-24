import { Link } from 'react-router-dom';
import { ArrowRight, ArrowUp } from 'lucide-react';
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
  {
    title: 'Store',
    links: [
      { to: '/about', label: 'About us' },
      { to: '/contact', label: 'Contact' },
      { to: '/orders', label: 'Your orders' },
      { to: '/cart', label: 'Cart' },
    ],
  },
];

// Pages the shop does not have yet; listed so the footer reads like a real one, but not links
const help = ['Shipping & payments', 'Returns & complaints', 'Terms & conditions', 'Privacy policy'];

/** An inverted footer: a last call to shop, the site map, the address and an oversized wordmark. */
const Footer = () => (
  <footer className="mt-24 bg-primary pb-28 text-primary-foreground dark:border-t dark:bg-card dark:text-card-foreground md:pb-0">
    <div className="align-element grid gap-14 pt-20 lg:grid-cols-12">
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

      <div className="grid grid-cols-2 gap-10 sm:grid-cols-4 lg:col-span-7">
        {columns.map((column) => (
          <div key={column.title}>
            <h4 className="eyebrow text-primary-foreground/50 dark:text-muted-foreground">{column.title}</h4>
            <ul className="mt-5 space-y-3 text-sm">
              {column.links.map((link) => (
                <li key={link.label}>
                  <Link to={link.to} className="link-underline">
                    {link.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        ))}
        <div>
          <h4 className="eyebrow text-primary-foreground/50 dark:text-muted-foreground">Help</h4>
          <ul className="mt-5 space-y-3 text-sm text-primary-foreground/60 dark:text-muted-foreground">
            {help.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </div>
        <div>
          <h4 className="eyebrow text-primary-foreground/50 dark:text-muted-foreground">Visit</h4>
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

    <div aria-hidden className="overflow-hidden">
      <p className="display select-none text-center text-[27vw] leading-[0.72] tracking-tighter text-primary-foreground/[0.07] dark:text-foreground/[0.06]">
        store.
      </p>
    </div>
  </footer>
);

export default Footer;
