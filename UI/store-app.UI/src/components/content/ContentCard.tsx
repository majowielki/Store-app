import { Link } from 'react-router-dom';
import ResponsiveImage from '@/components/ResponsiveImage';
import { ArrowUpRight } from 'lucide-react';
import { headingTag, type HeadingLevel } from '@/lib/headings';
import { cn } from '@/lib/utils';

interface ContentCardProps {
  to: string;
  image: string;
  title: string;
  eyebrow?: string;
  text?: string;
  className?: string;
  /** Wider picture for a featured card. */
  wide?: boolean;
  /** A card straight under the page title is an h2; one in a section of a page (the default), an h3. */
  headingLevel?: HeadingLevel;
}

/** A picture, a title and a line of text leading to an editorial page. */
const ContentCard = ({ to, image, title, eyebrow, text, className, wide = false, headingLevel = 3 }: ContentCardProps) => {
  const Heading = headingTag(headingLevel);
  return (
    <Link to={to} className={cn('group block rounded-3xl focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-4', className)}>
      <div className={cn('overflow-hidden rounded-3xl bg-muted', wide ? 'aspect-video' : 'aspect-4/3')}>
        <ResponsiveImage size="third" placeholder src={image} alt="" loading="lazy" className="h-full w-full object-cover transition-transform duration-700 ease-smooth group-hover:scale-105" />
      </div>
      <div className="mt-5 flex items-start justify-between gap-4">
        <div>
          {eyebrow && <p className="eyebrow">{eyebrow}</p>}
          <Heading className="display mt-2 text-3xl leading-tight">{title}</Heading>
          {text && <p className="mt-2 max-w-md text-sm leading-relaxed text-muted-foreground">{text}</p>}
        </div>
        <ArrowUpRight className="mt-2 h-5 w-5 shrink-0 transition-transform duration-300 group-hover:-translate-y-0.5 group-hover:translate-x-0.5" />
      </div>
    </Link>
  );
};

export default ContentCard;
