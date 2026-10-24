import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import SplitText from './SplitText';

describe('a line of text split into rising words', () => {
  it('is read once, whole, while the words are drawn one by one', () => {
    const { container } = render(<SplitText as="h2" text="Rooms made slowly" shown={false} />);

    expect(screen.getByRole('heading', { name: 'Rooms made slowly' })).toBeInTheDocument();
    const words = container.querySelectorAll('.split-word');
    expect([...words].map((word) => word.textContent)).toEqual(['Rooms', 'made', 'slowly']);
    expect(words[0].closest('[aria-hidden]')).not.toBeNull();
  });

  it('marks itself shown, so the words rise', () => {
    const { container, rerender } = render(<SplitText text="Rooms made slowly" shown={false} />);
    expect(container.firstChild).toHaveAttribute('data-split', '');
    rerender(<SplitText text="Rooms made slowly" shown />);
    expect(container.firstChild).toHaveAttribute('data-split', 'shown');
  });
});
