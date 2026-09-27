import type { ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import { infoPages } from '@/content/pages';
import { cn } from '@/lib/utils';

interface InfoPageProps {
  eyebrow: string;
  title: string;
  lead: string;
  children: ReactNode;
}

/** The layout of the help and legal pages: a side list of all of them and the text itself. */
const InfoPage = ({ eyebrow, title, lead, children }: InfoPageProps) => (
  <div className="grid gap-12 lg:grid-cols-12">
    <nav aria-label="Help and legal pages" className="lg:col-span-3">
      <ul className="flex flex-wrap gap-2 lg:sticky lg:top-28 lg:flex-col lg:gap-1">
        {infoPages.map((page) => (
          <li key={page.to}>
            <NavLink
              to={page.to}
              className={({ isActive }) =>
                cn(
                  'block rounded-full px-4 py-2 text-sm transition-colors lg:rounded-xl',
                  isActive ? 'bg-foreground text-background' : 'text-muted-foreground hover:bg-muted hover:text-foreground',
                )
              }
            >
              {page.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
    <article className="lg:col-span-8 lg:col-start-5">
      <p className="eyebrow animate-fade-up">{eyebrow}</p>
      <h1 className="display mt-4 animate-fade-up text-5xl [animation-delay:80ms] md:text-6xl">{title}</h1>
      <p className="mt-6 animate-fade-up text-lg leading-relaxed text-muted-foreground [animation-delay:160ms]">{lead}</p>
      <div className="mt-12 space-y-10">{children}</div>
    </article>
  </div>
);

/** A titled block of an information page. */
export const InfoSection = ({ title, children }: { title: string; children: ReactNode }) => (
  <section>
    <h2 className="display text-3xl">{title}</h2>
    <div className="mt-4 space-y-4 leading-relaxed text-foreground/85">{children}</div>
  </section>
);

/** The note every legal page carries: this is a portfolio shop. */
export const DemoNote = () => (
  <p className="rounded-2xl border border-brand/40 bg-brand/5 p-5 text-sm leading-relaxed">
    This store is a portfolio project. Nothing is sold or shipped and no money is taken: orders exist only to show how
    the shop works, and payments are simulated.
  </p>
);

export default InfoPage;
