import { useRef, type ReactNode } from 'react';
import { X } from 'lucide-react';
import { useFocusOnNavigate } from '@/hooks/use-focus-on-navigate';
import { MAIN_CONTENT_ID } from '@/lib/focus';
import { closeAuthPage } from '@/utils/closeAuthPage';
import Logo from './Logo';

interface AuthShellProps {
  eyebrow: string;
  title: ReactNode;
  lead: string;
  image: string;
  children: ReactNode;
}

/** The sign-in and registration pages: a photograph on one half, the form on the other. */
const AuthShell = ({ eyebrow, title, lead, image, children }: AuthShellProps) => {
  const main = useRef<HTMLElement>(null);
  // From sign-in to registration and back: the focus goes on from the top of the new form
  useFocusOnNavigate(main);

  return (
    <main id={MAIN_CONTENT_ID} ref={main} tabIndex={-1} className="grid min-h-screen focus:outline-none lg:grid-cols-2">
      <div className="relative hidden overflow-hidden bg-muted lg:block">
        <img src={image} alt="" className="absolute inset-0 h-full w-full animate-zoom-out object-cover" />
        <div className="absolute inset-0 bg-linear-to-t from-black/70 via-black/10 to-black/30" />
        <Logo className="absolute left-10 top-8 text-white" />
        <figure className="absolute inset-x-10 bottom-10 max-w-lg animate-fade-up text-white [animation-delay:300ms]">
          <blockquote className="display text-4xl leading-[1.1]">Good rooms are made slowly — one well-chosen piece at a time.</blockquote>
          <figcaption className="mt-4 text-xs uppercase tracking-[0.2em] text-white/70">The Store team</figcaption>
        </figure>
      </div>

      <div className="flex flex-col px-6 py-6 sm:px-12">
        <div className="flex items-center justify-between">
          <Logo className="lg:invisible" />
          <button
            type="button"
            onClick={closeAuthPage}
            className="grid h-10 w-10 place-items-center rounded-full border transition-colors hover:border-foreground hover:bg-foreground hover:text-background"
            title="Close"
            aria-label="Close"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
        <div className="mx-auto flex w-full max-w-sm flex-1 flex-col justify-center py-12">
          <p className="eyebrow animate-fade-up">{eyebrow}</p>
          <h1 className="display mt-3 animate-fade-up text-5xl [animation-delay:60ms]">{title}</h1>
          <p className="mt-3 animate-fade-up text-muted-foreground [animation-delay:120ms]">{lead}</p>
          <div className="mt-10 animate-fade-up [animation-delay:180ms]">{children}</div>
        </div>
      </div>
    </main>
  );
};

export default AuthShell;
