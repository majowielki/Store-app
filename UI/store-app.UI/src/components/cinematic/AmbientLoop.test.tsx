import { render } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { REDUCED_MOTION_QUERY, WIDE_SCREEN_QUERY } from '@/lib/motion';
import AmbientLoop from './AmbientLoop';

const realMatchMedia = window.matchMedia;
const realObserver = window.IntersectionObserver;
const screenWith = (...queries: string[]) => {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({ matches: queries.includes(query), media: query })) as typeof window.matchMedia;
};
/** An IntersectionObserver that reports every observed element on screen at once. */
class OnScreen {
  constructor(private readonly callback: IntersectionObserverCallback) {}
  observe(element: Element) {
    this.callback([{ isIntersecting: true, target: element } as IntersectionObserverEntry], this as unknown as IntersectionObserver);
  }
  disconnect() {}
}

describe('a still picture that comes alive', () => {
  afterEach(() => {
    window.matchMedia = realMatchMedia;
    window.IntersectionObserver = realObserver;
  });

  it('adds its loop once near the screen, on a large screen with motion allowed', () => {
    screenWith(WIDE_SCREEN_QUERY);
    window.IntersectionObserver = OnScreen as unknown as typeof IntersectionObserver;
    const { container } = render(<AmbientLoop poster="/room.webp" video="/room.mp4" flag="emptyRoom" />);
    expect(container.querySelector('img')).toHaveAttribute('src', '/room.webp');
    expect(container.querySelector('video')).toHaveAttribute('src', '/room.mp4');
  });

  it('stays a picture on a phone (unless allowed there) and with less motion', () => {
    window.IntersectionObserver = OnScreen as unknown as typeof IntersectionObserver;
    for (const [queries, phones] of [[[], false], [[WIDE_SCREEN_QUERY, REDUCED_MOTION_QUERY], true]] as const) {
      screenWith(...queries);
      const { container, unmount } = render(<AmbientLoop poster="/room.webp" video="/room.mp4" flag="emptyRoom" phones={phones} />);
      expect(container.querySelector('video')).toBeNull();
      unmount();
    }
  });
});
