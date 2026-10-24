import { fireEvent, render } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { REDUCED_MOTION_QUERY, WIDE_SCREEN_QUERY } from '@/lib/motion';
import AuthFilm, { type Film } from './AuthFilm';

const film: Film = { video: '/entrance.mp4', poster: '/entrance.webp', focus: '50% 50%' };
const realMatchMedia = window.matchMedia;

/** A screen answering true to the given media queries only. */
const screenWith = (...queries: string[]) => {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({ matches: queries.includes(query), media: query })) as typeof window.matchMedia;
};

describe('the film of the sign-in and registration pages', () => {
  beforeEach(() => {
    sessionStorage.clear();
    vi.spyOn(HTMLMediaElement.prototype, 'play').mockResolvedValue(undefined);
  });
  afterEach(() => {
    window.matchMedia = realMatchMedia;
    vi.restoreAllMocks();
  });

  it('plays once on a large screen and then gives way to the picture', () => {
    screenWith(WIDE_SCREEN_QUERY);
    const settled = vi.fn();
    const { container, unmount } = render(<AuthFilm image="/detail.webp" film={film} onSettled={settled} />);

    const video = container.querySelector('video')!;
    expect(video).toHaveAttribute('src', film.video);
    expect(container.querySelector('img')).toHaveClass('opacity-0');
    expect(settled).not.toHaveBeenCalled();

    fireEvent.ended(video);
    expect(container.querySelector('img')).toHaveClass('animate-film-detail');
    expect(settled).toHaveBeenCalledOnce();
    unmount();

    // The next visit in the same tab opens on the picture
    const again = render(<AuthFilm image="/detail.webp" film={film} onSettled={vi.fn()} />);
    expect(again.container.querySelector('video')).toBeNull();
  });

  it('shows only the picture on a small screen or to visitors who asked for less motion', () => {
    for (const queries of [[], [WIDE_SCREEN_QUERY, REDUCED_MOTION_QUERY]]) {
      screenWith(...queries);
      const settled = vi.fn();
      const { container, unmount } = render(<AuthFilm image="/detail.webp" film={film} onSettled={settled} />);
      expect(container.querySelector('video')).toBeNull();
      expect(container.querySelector('img')).toHaveClass('animate-zoom-out');
      expect(settled).toHaveBeenCalledOnce();
      unmount();
    }
  });

  it('falls back to the picture when the clip cannot be played', () => {
    screenWith(WIDE_SCREEN_QUERY);
    const { container } = render(<AuthFilm image="/detail.webp" film={film} onSettled={vi.fn()} />);
    fireEvent.error(container.querySelector('video')!);
    expect(container.querySelector('video')).toBeNull();
    expect(container.querySelector('img')).not.toHaveClass('opacity-0');
  });
});
