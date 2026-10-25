import { act, fireEvent } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { REDUCED_MOTION_QUERY, WIDE_SCREEN_QUERY } from '@/lib/motion';
import { renderWithStore } from '@/test/render';
import HeroLight from './HeroLight';

const realMatchMedia = window.matchMedia;
const screenWith = (...queries: string[]) => {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({ matches: queries.includes(query), media: query })) as typeof window.matchMedia;
};
const morning = { image: '/room.webp', video: '/light.mp4', width: 1200, height: 1800 };
const evening = { image: '/dusk.webp', video: '/dusk.mp4', width: 1200, height: 1200 };
const hero = () => renderWithStore(<HeroLight morning={morning} evening={evening} alt="A room" />);

describe('the hero picture with the moving light', () => {
  afterEach(() => {
    window.matchMedia = realMatchMedia;
  });

  it('keeps the picture alone until the visitor first moves, then fades the light in when it plays', () => {
    screenWith(WIDE_SCREEN_QUERY);
    const { container } = hero();
    expect(container.querySelector('img')).toHaveAttribute('fetchpriority', 'high');
    expect(container.querySelector('video')).toBeNull();

    act(() => {
      window.dispatchEvent(new Event('pointermove'));
    });
    const video = container.querySelector('video')!;
    expect(video).toHaveAttribute('src', '/light.mp4');
    expect(video).toHaveClass('opacity-0');
    fireEvent.playing(video);
    expect(video).toHaveClass('opacity-100');
  });

  it('shows the room at sunset in the dark theme', () => {
    screenWith(WIDE_SCREEN_QUERY);
    const { container } = renderWithStore(<HeroLight morning={morning} evening={evening} alt="A room" />, { preloadedState: { theme: { theme: 'dark' } } });
    expect(container.querySelector('img')).toHaveAttribute('src', '/dusk.webp');
    act(() => {
      window.dispatchEvent(new Event('pointermove'));
    });
    expect(container.querySelector('video')).toHaveAttribute('src', '/dusk.mp4');
  });

  it('never fetches the light on a small screen or with less motion', () => {
    for (const queries of [[], [WIDE_SCREEN_QUERY, REDUCED_MOTION_QUERY]]) {
      screenWith(...queries);
      const { container, unmount } = hero();
      act(() => {
        window.dispatchEvent(new Event('scroll'));
      });
      expect(container.querySelector('video')).toBeNull();
      unmount();
    }
  });
});
