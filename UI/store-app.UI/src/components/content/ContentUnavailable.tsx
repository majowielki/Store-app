import { Link } from 'react-router-dom';
import { HttpStatus, isApiError } from '@/api/problem';
import { Button } from '@/components/ui/button';

/** What a content page shows when its entry cannot be read: 404 for an unknown slug. */
const ContentUnavailable = ({ error, what, back }: { error: unknown; what: string; back: { to: string; label: string } }) => {
  const notFound = isApiError(error) && error.status === HttpStatus.NotFound;
  return (
    <div className="grid place-items-center py-24 text-center">
      <p className="eyebrow">{notFound ? '404' : 'Unavailable'}</p>
      <h1 className="display mt-4 text-5xl">{notFound ? `${what} not found` : `${what} unavailable`}</h1>
      <Button asChild variant="outline" className="mt-8">
        <Link to={back.to}>{back.label}</Link>
      </Button>
    </div>
  );
};

export default ContentUnavailable;
