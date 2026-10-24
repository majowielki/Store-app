import { act, fireEvent, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithStore } from '@/test/render';
import AnnouncementBar from './AnnouncementBar';

const shownPerk = () => screen.getAllByRole('listitem').find((item) => item.dataset.shown === 'true')?.textContent;

describe('the announcement bar', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('is a landmark of its own listing every perk, and its turning can be paused and played again', () => {
    renderWithStore(<AnnouncementBar />);

    expect(screen.getByRole('complementary', { name: 'What every order gets' })).toBeInTheDocument();
    expect(screen.getAllByRole('listitem')).toHaveLength(4);
    const first = shownPerk();
    act(() => vi.advanceTimersByTime(5000));
    const second = shownPerk();
    expect(second).not.toBe(first);

    const pause = screen.getByRole('button', { name: 'Pause the offers' });
    fireEvent.click(pause);
    expect(pause).toHaveAttribute('aria-pressed', 'true');
    act(() => vi.advanceTimersByTime(15000));
    expect(shownPerk()).toBe(second);

    fireEvent.click(pause);
    expect(pause).toHaveAttribute('aria-pressed', 'false');
    act(() => vi.advanceTimersByTime(5000));
    expect(shownPerk()).not.toBe(second);
  });
});
