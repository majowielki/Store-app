import { setupServer } from 'msw/node';
import { handlers } from './handlers';

/** The API as the tests see it: the default handlers below, overridden per test with server.use(). */
export const server = setupServer(...handlers);
