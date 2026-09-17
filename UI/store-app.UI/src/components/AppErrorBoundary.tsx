import { Component, type ErrorInfo, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

interface State {
  failed: boolean;
}

/**
 * The last resort, outside the router: an error the route error pages did not catch (in the
 * providers or the toaster) shows this instead of a blank page. Errors inside routes are
 * handled by their errorElement (pages/Error.tsx, components/ErrorElement.tsx).
 */
export class AppErrorBoundary extends Component<Props, State> {
  state: State = { failed: false };

  static getDerivedStateFromError(): State {
    return { failed: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    // Nothing else can report it at this point
    // eslint-disable-next-line no-console
    console.error('Unrecoverable error', error, info.componentStack);
  }

  render(): ReactNode {
    if (!this.state.failed) return this.props.children;
    return (
      <main className="grid min-h-[100vh] place-items-center px-8 text-center">
        <div>
          <h1 className="text-3xl font-bold">Something went wrong</h1>
          <p className="mt-4">Reload the page to continue.</p>
          <button
            type="button"
            className="mt-8 rounded-md border px-4 py-2"
            onClick={() => window.location.reload()}
          >
            Reload
          </button>
        </div>
      </main>
    );
  }
}
