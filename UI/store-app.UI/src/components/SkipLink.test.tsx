import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { MAIN_CONTENT_ID } from '@/lib/focus';
import SkipLink from './SkipLink';

describe('the skip link', () => {
  it('is the first stop of the Tab key and takes the focus past the header to the content', async () => {
    const user = userEvent.setup();
    render(
      <>
        <SkipLink />
        <header>
          <a href="/products">Shop all</a>
        </header>
        <main id={MAIN_CONTENT_ID} tabIndex={-1}>
          <h1>Home</h1>
        </main>
      </>,
    );

    await user.tab();
    expect(screen.getByRole('link', { name: 'Skip to content' })).toHaveFocus();

    await user.keyboard('{Enter}');
    expect(screen.getByRole('main')).toHaveFocus();
    // The router owns the address: no #main-content is added to it
    expect(window.location.hash).toBe('');
  });
});
