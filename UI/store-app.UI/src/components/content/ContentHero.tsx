import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';

interface ContentHeroProps {
  eyebrow: string;
  title: string;
  lead?: string;
  image: string;
  back: { to: string; label: string };
  /** Small facts under the lead (a date, a place). */
  meta?: ReactNode;
}

/** The top of an editorial page: the way back, the title over a wide cover picture. */
const ContentHero = ({ eyebrow, title, lead, image, back, meta }: ContentHeroProps) => (
  <header>
    <Link to={back.to} className="group inline-flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-foreground">
      <ArrowLeft className="h-4 w-4 transition-transform group-hover:-translate-x-0.5" />
      {back.label}
    </Link>
    <div className="mt-8 grid gap-8 lg:grid-cols-12 lg:items-end">
      <div className="lg:col-span-7">
        <p className="eyebrow animate-fade-up">{eyebrow}</p>
        <h1 className="display mt-4 animate-fade-up text-[clamp(2.5rem,6vw,5.5rem)] leading-[0.95] [animation-delay:80ms]">{title}</h1>
      </div>
      {(lead || meta) && (
        <div className="animate-fade-up space-y-4 [animation-delay:160ms] lg:col-span-5">
          {lead && <p className="text-lg leading-relaxed text-muted-foreground">{lead}</p>}
          {meta && <div className="text-sm text-muted-foreground">{meta}</div>}
        </div>
      )}
    </div>
    <div className="mt-10 aspect-3/2 animate-fade-up overflow-hidden rounded-4xl bg-muted [animation-delay:220ms] md:aspect-21/9">
      <img src={image} alt="" className="h-full w-full object-cover" />
    </div>
  </header>
);

export default ContentHero;
