import { createRoot } from 'react-dom/client';
import { Provider } from 'react-redux';
import App from './App.tsx';
import './index.css';
import { AppErrorBoundary } from '@/components/AppErrorBoundary';
import { Toaster } from '@/components/ui/toaster';
import { restoreSession } from '@/features/session/sessionThunks';
import { store } from './store';

// The session is continued from the refresh cookie before anything renders; route guards
// wait for the outcome (see routes/guards.ts)
store.dispatch(restoreSession());

createRoot(document.getElementById('root')!).render(
  <AppErrorBoundary>
    <Provider store={store}>
      <Toaster />
      <App />
    </Provider>
  </AppErrorBoundary>,
);
