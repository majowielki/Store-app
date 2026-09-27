import ReactMarkdown, { type Components } from 'react-markdown';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';

// The shop's typography for editorial text; links inside the shop stay in the single-page app
const components: Components = {
  h2: ({ children }) => <h2 className="display mt-12 text-3xl md:text-4xl">{children}</h2>,
  h3: ({ children }) => <h3 className="mt-8 text-xl font-medium">{children}</h3>,
  p: ({ children }) => <p className="mt-5 leading-relaxed text-foreground/85">{children}</p>,
  ul: ({ children }) => <ul className="mt-5 list-disc space-y-2 pl-6 text-foreground/85 marker:text-brand">{children}</ul>,
  ol: ({ children }) => <ol className="mt-5 list-decimal space-y-2 pl-6 text-foreground/85 marker:text-muted-foreground">{children}</ol>,
  strong: ({ children }) => <strong className="font-semibold text-foreground">{children}</strong>,
  blockquote: ({ children }) => <blockquote className="mt-8 border-l-2 border-brand pl-6 font-serif text-xl italic">{children}</blockquote>,
  a: ({ href = '', children }) =>
    href.startsWith('/') ? (
      <Link to={href} className="link-underline font-medium">
        {children}
      </Link>
    ) : (
      <a href={href} target="_blank" rel="noopener noreferrer" className="link-underline font-medium">
        {children}
      </a>
    ),
};

/**
 * Markdown written in the admin panel, rendered with the shop's typography. Raw HTML is dropped
 * rather than rendered, so an article can hold links and emphasis but never a script.
 */
const Markdown = ({ children, className }: { children: string; className?: string }) => (
  <div className={cn('text-[1.05rem] [&>*:first-child]:mt-0', className)}>
    <ReactMarkdown skipHtml components={components}>
      {children}
    </ReactMarkdown>
  </div>
);

export default Markdown;
