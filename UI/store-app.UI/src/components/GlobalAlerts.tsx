import { useAppDispatch, useAppSelector } from '@/hooks';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { removeAlert } from '@/features/alerts/alertsSlice';
import { X } from 'lucide-react';

const GlobalAlerts = () => {
  const alerts = useAppSelector((s) => s.alerts.items);
  const dispatch = useAppDispatch();

  if (!alerts.length) return null;

  return (
    <div className="align-element px-4 pt-4 space-y-2">
      {alerts.map((a) => (
        <Alert key={a.id} variant={a.variant ?? 'default'} className="relative pr-10">
          {(a.title || a.description) && (
            <div>
              {a.title && <AlertTitle>{a.title}</AlertTitle>}
              {a.description && <AlertDescription>{a.description}</AlertDescription>}
            </div>
          )}
          <button
            aria-label="Dismiss"
            className="absolute right-2 top-2 text-muted-foreground hover:text-foreground"
            onClick={() => dispatch(removeAlert(a.id))}
          >
            <X className="h-4 w-4" />
          </button>
        </Alert>
      ))}
    </div>
  );
};

export default GlobalAlerts;
