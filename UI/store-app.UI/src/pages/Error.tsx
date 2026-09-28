import { useRouteError, Link, isRouteErrorResponse } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { HttpStatus } from "@/api/problem";
import { Button } from "@/components/ui/button";
import Logo from "@/components/Logo";
import { usePageMeta } from "@/seo";

const Error = () => {
  const error = useRouteError();
  const notFound = isRouteErrorResponse(error) && error.status === HttpStatus.NotFound;
  usePageMeta({ title: notFound ? "Page not found" : "Something went wrong", noindex: true });

  return (
    <main className="relative grid min-h-screen place-items-center overflow-hidden px-6">
      <Logo className="absolute left-6 top-6 sm:left-10 sm:top-8" />
      <p aria-hidden className="display pointer-events-none absolute select-none text-[42vw] leading-none text-foreground/4">
        {notFound ? "404" : "oops"}
      </p>
      <div className="relative text-center">
        <p className="eyebrow animate-fade-up">{notFound ? "Error 404" : "Something went wrong"}</p>
        <h1 className="display mt-4 animate-fade-up text-5xl [animation-delay:80ms] sm:text-7xl">
          {notFound ? <>This room is <em className="text-brand">empty.</em></> : <>There was an <em className="text-brand">error…</em></>}
        </h1>
        <p className="mx-auto mt-6 max-w-md animate-fade-up text-lg text-muted-foreground [animation-delay:160ms]">
          {notFound
            ? "Sorry, we could not find the page you are looking for."
            : "The page could not be shown. Try again in a moment, or start over from the home page."}
        </p>
        <div className="mt-10 animate-fade-up [animation-delay:240ms]">
          <Button asChild size="lg" className="group">
            <Link to="/">
              <ArrowLeft className="transition-transform duration-300 group-hover:-translate-x-1" />
              Go back home
            </Link>
          </Button>
        </div>
      </div>
    </main>
  );
}
export default Error;
