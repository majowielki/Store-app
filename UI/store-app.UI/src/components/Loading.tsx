import { Skeleton } from "./ui/skeleton";

/** Placeholders shaped like the product tiles, while a page's data is on its way. */
const Loading = () => {
  return (
    <div className="grid gap-x-6 gap-y-12 sm:grid-cols-2 xl:grid-cols-3" aria-busy="true" aria-label="Loading">
      {Array.from({ length: 6 }).map((_, index) => {
        return (
          <div key={index} className="flex flex-col">
            <Skeleton className="aspect-[5/4] w-full rounded-2xl" />
            <Skeleton className="mt-4 h-3 w-16" />
            <Skeleton className="mt-2 h-4 w-2/3" />
          </div>
        );
      })}
    </div>
  );
}
export default Loading;
