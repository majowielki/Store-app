import { isRouteErrorResponse, useRouteError } from 'react-router-dom';
import { errorMessage } from '@/api/problem';

/** Shown in place of a page (inside the layout) when rendering it threw. */
const ErrorElement = () => {
  const error = useRouteError();
  const message = isRouteErrorResponse(error) ? `${error.status} ${error.statusText}` : errorMessage(error);
  return (
    <div className="py-12">
      <h4 className="font-bold text-4xl">There was an error...</h4>
      <p className="mt-4 text-muted-foreground">{message}</p>
    </div>
  );
};
export default ErrorElement;
