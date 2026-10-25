import { useRouteError, Link, isRouteErrorResponse } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { HttpStatus } from "@/api/problem";
import emptyRoom from "@/assets/cinematic/empty-room.mp4";
import emptyRoomPoster from "@/assets/cinematic/empty-room-poster.webp";
import AmbientLoop from "@/components/cinematic/AmbientLoop";
import { Button } from "@/components/ui/button";
import Logo from "@/components/Logo";
import { usePageMeta } from "@/seo";
import { categories, categoryHref } from "@/utils/categories";

const HomeButton = () => (
  <Button asChild size="lg" className="group">
    <Link to="/">
      <ArrowLeft className="transition-transform duration-300 group-hover:-translate-x-1" />
      Go back home
    </Link>
  </Button>
);

/**
 * A page that is not there: an empty room, not yet furnished, with the light and the curtain
 * moving in it - and the way into the rooms the shop does have.
 */
const NotFound = () => (
  <main className="relative grid min-h-svh place-items-center overflow-hidden px-6 py-24">
    <AmbientLoop poster={emptyRoomPoster} video={emptyRoom} flag="emptyRoom" phones className="absolute inset-0" />
    {/* A soft veil over the room, lighter in the middle where the text stands on the bare wall */}
    <div aria-hidden className="absolute inset-0 bg-[radial-gradient(ellipse_at_center,hsl(var(--background)/0.55),hsl(var(--background)/0.15)_70%)]" />
    <Logo className="absolute left-6 top-6 sm:left-10 sm:top-8" />
    <div className="relative max-w-2xl text-center">
      <p className="eyebrow animate-fade-up text-foreground/70">Error 404</p>
      <h1 className="display mt-4 animate-fade-up text-5xl [animation-delay:80ms] sm:text-7xl">
        This room is <em className="text-brand">empty.</em>
      </h1>
      <p className="mx-auto mt-6 max-w-md animate-fade-up text-lg text-foreground/75 [animation-delay:160ms]">
        This page has moved out, or never lived here. The rooms are where you left them.
      </p>
      <div className="mt-10 animate-fade-up [animation-delay:240ms]">
        <HomeButton />
      </div>
      <nav aria-label="Rooms of the shop" className="mt-10 animate-fade-up [animation-delay:320ms]">
        <ul className="flex flex-wrap justify-center gap-2">
          {categories.map((category) => (
            <li key={category.slug}>
              <Link
                to={categoryHref(category)}
                className="inline-flex rounded-full border border-foreground/15 bg-background/70 px-4 py-2 text-sm backdrop-blur-sm transition-colors hover:border-foreground hover:bg-foreground hover:text-background"
              >
                {category.label}
              </Link>
            </li>
          ))}
        </ul>
      </nav>
    </div>
  </main>
);

const Error = () => {
  const error = useRouteError();
  const notFound = isRouteErrorResponse(error) && error.status === HttpStatus.NotFound;
  usePageMeta({ title: notFound ? "Page not found" : "Something went wrong", noindex: true });
  if (notFound) return <NotFound />;

  return (
    <main className="relative grid min-h-screen place-items-center overflow-hidden px-6">
      <Logo className="absolute left-6 top-6 sm:left-10 sm:top-8" />
      <p aria-hidden className="display pointer-events-none absolute select-none text-[42vw] leading-none text-foreground/4">
        oops
      </p>
      <div className="relative text-center">
        <p className="eyebrow animate-fade-up">Something went wrong</p>
        <h1 className="display mt-4 animate-fade-up text-5xl [animation-delay:80ms] sm:text-7xl">
          There was an <em className="text-brand">error…</em>
        </h1>
        <p className="mx-auto mt-6 max-w-md animate-fade-up text-lg text-muted-foreground [animation-delay:160ms]">
          The page could not be shown. Try again in a moment, or start over from the home page.
        </p>
        <div className="mt-10 animate-fade-up [animation-delay:240ms]">
          <HomeButton />
        </div>
      </div>
    </main>
  );
}
export default Error;
