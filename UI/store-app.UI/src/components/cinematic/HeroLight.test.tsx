import { act, fireEvent, render } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { REDUCED_MOTION_QUERY, WIDE_SCREEN_QUERY } from '@/lib/motion';
import HeroLight from './HeroLight';

const realMatchMedia = window.matchMedia;
const screenWith = (...queries: string[]) => {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({ matches: queries.includes(query), media: query })) as typeof window.matchMedia;
};
const hero = () => render(<HeroLight image="/room.webp" alt="A room" width={1200} height={1800} video="/light.mp4" />);

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
