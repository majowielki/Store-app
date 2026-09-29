import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRef } from 'react';
import { Link, MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { keepFocusOnNewPage, MAIN_CONTENT_ID } from '@/lib/focus';
import { useFocusOnNavigate } from './use-focus-on-navigate';

/** A layout like the shop's: links outside the content, the page inside a focusable <main>. */
const Layout = () => {
  const main = useRef<HTMLElement>(null);
  useFocusOnNavigate(main);
  return (
    <>
      <nav>
        <Link to="/products">Products</Link>
        <Link to="/?sort=newest">Newest first</Link>
      </nav>
      <main id={MAIN_CONTENT_ID} ref={main} tabIndex={-1}>
        <Routes>
          <Route path="/" element={<h1>Home</h1>} />
          <Route path="/products" element={<h1>Products</h1>} />
        </Routes>
      </main>
    </>
  );
};

const renderLayout = () =>
  render(
    <MemoryRouter initialEntries={['/']}>
      <Layout />
    </MemoryRouter>,
  );

describe('the focus on a change of page', () => {
  it('stays where the browser put it on the first page of the visit', () => {
    renderLayout();

    expect(document.body).toHaveFocus();
  });

  it('moves to the content of the new page, away from the link that was followed', async () => {
    const user = userEvent.setup();
    renderLayout();

    await user.click(screen.getByRole('link', { name: 'Products' }));

    expect(screen.getByRole('heading', { name: 'Products' })).toBeInTheDocument();
    expect(screen.getByRole('main')).toHaveFocus();
  });

  it('stays on the control when only the query changes (sorting, filters, the pages of a list)', async () => {
    const user = userEvent.setup();
    renderLayout();

    await user.click(screen.getByRole('link', { name: 'Newest first' }));

    expect(screen.getByRole('link', { name: 'Newest first' })).toHaveFocus();
  });
});

describe('the focus when a dialog closes', () => {
  const closing = () => new Event('focusOutside', { cancelable: true });

  it('stays on the new page a link inside the dialog opened', async () => {
    const user = userEvent.setup();
    renderLayout();
    await user.click(screen.getByRole('link', { name: 'Products' }));
    const event = closing();

    keepFocusOnNewPage(event);

    expect(event.defaultPrevented).toBe(true);
  });

  it('goes back to the button that opened the dialog when the page did not change', () => {
    renderLayout();
    const event = closing();

    keepFocusOnNewPage(event);

    expect(event.defaultPrevented).toBe(false);
  });
});
