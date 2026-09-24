import { Link, isRouteErrorResponse, useRouteError } from 'react-router-dom';
import { errorMessage } from '@/api/problem';
import { Button } from './ui/button';

/** Shown in place of a page (inside the layout) when rendering it threw. */
const ErrorElement = () => {
  const error = useRouteError();
  const message = isRouteErrorResponse(error) ? `${error.status} ${error.statusText}` : errorMessage(error);
  return (
    <div className="grid place-items-center py-24 text-center">
      <p className="eyebrow">Something went wrong</p>
      <h4 className="display mt-4 text-5xl">There was an error...</h4>
      <p className="mt-4 max-w-md text-muted-foreground">{message}</p>
      <Button asChild variant="outline" className="mt-8">
        <Link to="/">Back to the home page</Link>
      </Button>
    </div>
  );
};
export default ErrorElement;
