import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { renderWithStore } from '@/test/render';
import AnnouncementBar from './AnnouncementBar';

describe('the announcement bar', () => {
  it('is a landmark of its own and its ticker can be paused and played again', async () => {
    const user = userEvent.setup();
    renderWithStore(<AnnouncementBar />);

    expect(screen.getByRole('complementary', { name: 'What every order gets' })).toBeInTheDocument();
    const pause = screen.getByRole('button', { name: 'Pause the offers' });
    const ticker = screen.getAllByText('30-day returns, no questions asked')[0].closest('.animate-marquee');
    expect(ticker).not.toHaveClass('paused');

    await user.click(pause);
    expect(pause).toHaveAttribute('aria-pressed', 'true');
    expect(ticker).toHaveClass('paused');

    await user.click(pause);
    expect(pause).toHaveAttribute('aria-pressed', 'false');
    expect(ticker).not.toHaveClass('paused');
  });
});
