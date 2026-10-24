import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import SplitText from './SplitText';

describe('a line of text split into rising words', () => {
  it('is read once, whole, while the words are drawn one by one', () => {
    const { container } = render(
      <SplitText as="h2" shown={false}>
        Rooms made <em>slowly</em>
      </SplitText>,
    );

    expect(screen.getByRole('heading', { name: 'Rooms made slowly' })).toBeInTheDocument();
    const words = container.querySelectorAll('.split-word');
    expect([...words].map((word) => word.textContent)).toEqual(['Rooms', 'made', 'slowly']);
    expect(words[0].closest('[aria-hidden]')).not.toBeNull();
    // The emphasis stays around its word
    expect(words[2].querySelector('em')).toHaveTextContent('slowly');
  });

  it('marks itself shown, so the words rise', () => {
    const { container, rerender } = render(<SplitText shown={false}>Rooms made slowly</SplitText>);
    expect(container.firstChild).toHaveAttribute('data-split', '');
    rerender(<SplitText shown>Rooms made slowly</SplitText>);
    expect(container.firstChild).toHaveAttribute('data-split', 'shown');
  });
});
