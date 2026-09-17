import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { admin, session } from '@/test/fixtures';
import { api, json, problemResponse } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import Login from './Login';

/** The login page with the two places it sends people to. */
const App = () => (
  <Routes>
    <Route path="/login" element={<Login />} />
    <Route path="/" element={<h1>Shop</h1>} />
    <Route path="/admin" element={<h1>Admin panel</h1>} />
  </Routes>
);

const fillAndSubmit = async (email: string, password: string) => {
  await userEvent.type(screen.getByLabelText('email'), email);
  await userEvent.type(screen.getByLabelText('password'), password);
  await userEvent.click(screen.getByRole('button', { name: 'Login' }));
};

describe('Login page', () => {
  it('signs a customer in and sends them to the shop', async () => {
    const { store } = renderWithStore(<App />, { route: '/login' });

    await fillAndSubmit('anna@example.com', 'secret-1!');

    expect(await screen.findByRole('heading', { name: 'Shop' })).toBeInTheDocument();
    expect(store.getState().session.user?.email).toBe('anna@example.com');
    expect(await screen.findByText('Successfully logged in!')).toBeInTheDocument();
  });

  it('sends an administrator to the admin panel, judged by the roles the API returned', async () => {
    server.use(http.post(api('/auth/login'), () => json(session(admin))));
    renderWithStore(<App />, { route: '/login' });

    await fillAndSubmit('admin@example.com', 'secret-1!');

    expect(await screen.findByRole('heading', { name: 'Admin panel' })).toBeInTheDocument();
  });

  it('shows the problem the API answered with and stays on the page', async () => {
    server.use(http.post(api('/auth/login'), () => problemResponse(401, 'Invalid email or password')));
    const { store } = renderWithStore(<App />, { route: '/login' });

    await fillAndSubmit('anna@example.com', 'wrong-1!');

    expect(await screen.findByText('Invalid email or password')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Login' })).toBeEnabled();
    expect(store.getState().session.user).toBeNull();
  });

  it('validates the form before asking the API', async () => {
    let called = false;
    server.use(
      http.post(api('/auth/login'), () => {
        called = true;
        return json(session());
      }),
    );
    renderWithStore(<App />, { route: '/login' });

    await userEvent.click(screen.getByRole('button', { name: 'Login' }));

    expect(screen.getAllByText(/required|invalid|enter/i).length).toBeGreaterThan(0);
    expect(called).toBe(false);
  });
});
