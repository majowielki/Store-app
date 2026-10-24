import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { REDUCED_MOTION_QUERY, WIDE_SCREEN_QUERY } from '@/lib/motion';
import ScrollFilm from './ScrollFilm';

const film = { video: '/walk.mp4', poster: '/walk.webp' };
const realMatchMedia = window.matchMedia;
const screenWith = (...queries: string[]) => {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({ matches: queries.includes(query), media: query })) as typeof window.matchMedia;
};
const picture = () =>
  render(
    <ScrollFilm film={film}>
      <button type="button">Sofa, $1,499.99</button>
    </ScrollFilm>,
  );

describe('a picture walked into by scrolling', () => {
  afterEach(() => {
    window.matchMedia = realMatchMedia;
  });

  it('covers the picture with the walk until it is reached, and uncovers it for the keyboard at once', () => {
    screenWith(WIDE_SCREEN_QUERY);
    const { container } = picture();
    const cover = container.querySelector('[aria-hidden]')!;
    // jsdom lays nothing out: the picture sits at the top of a zero-height screen, short of the walk's end
    expect(cover.querySelector('img')).toHaveAttribute('src', film.poster);

    fireEvent.focus(screen.getByRole('button', { name: 'Sofa, $1,499.99' }));
    expect(cover).toHaveClass('opacity-0');
  });

  it('shows the picture alone on a small screen or to visitors who asked for less motion', () => {
    for (const queries of [[], [WIDE_SCREEN_QUERY, REDUCED_MOTION_QUERY]]) {
      screenWith(...queries);
      const { container, unmount } = picture();
      expect(container.querySelector('[aria-hidden]')).toBeNull();
      expect(screen.getByRole('button', { name: 'Sofa, $1,499.99' })).toBeInTheDocument();
      unmount();
    }
  });
});
