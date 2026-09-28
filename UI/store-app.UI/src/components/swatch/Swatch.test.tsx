import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it } from 'vitest';
import SelectProductColor from '@/components/SelectProductColor';
import FinishPicker from '@/pages/admin/FinishPicker';
import { finishes } from '@/test/fixtures';
import { renderWithStore } from '@/test/render';
import { isLightSwatch } from './contrast';
import FinishLabel from './FinishLabel';

const Picker = ({ colors }: { colors: string[] }) => {
  const [color, setColor] = useState(colors[0]);
  return <SelectProductColor colors={colors} productColor={color} setProductColor={setColor} />;
};

describe('swatches', () => {
  it('draws a pair split along the diagonal, each half with its surface, under the finish name', async () => {
    const { container } = renderWithStore(<FinishLabel color="black-steel-oak" />);

    expect(await screen.findByText('Black steel and oak')).toBeInTheDocument();
    const swatch = container.querySelector<HTMLElement>('[aria-hidden]')!;
    expect(swatch.style.backgroundImage).toContain('black-steel');
    const second = swatch.firstElementChild as HTMLElement;
    expect(second.style.backgroundImage).toContain('natural-oak');
    expect(second.style.clipPath).toBe('polygon(100% 0, 100% 100%, 0 100%)');
  });

  // An order placed before the finishes keeps a plain colour
  it('shows a colour the shop does not know as that CSS colour under its own name', async () => {
    const { container } = renderWithStore(<FinishLabel color="brown" />);

    expect(await screen.findByText('Brown')).toBeInTheDocument();
    const swatch = container.querySelector<HTMLElement>('[aria-hidden]')!;
    expect(swatch.style.backgroundColor).toBe('brown');
    expect(swatch.childElementCount).toBe(0);
  });

  it('names the colours to pick and ticks the chosen one', async () => {
    const user = userEvent.setup();
    renderWithStore(<Picker colors={['natural-oak', 'navy-linen']} />);

    const colours = await screen.findByRole('radiogroup', { name: 'Colour' });
    expect(await within(colours).findByRole('radio', { name: 'Natural oak' })).toHaveAttribute('aria-checked', 'true');
    await user.click(within(colours).getByRole('radio', { name: 'Navy linen' }));

    expect(within(colours).getByRole('radio', { name: 'Navy linen' })).toHaveAttribute('aria-checked', 'true');
    expect(screen.getByRole('heading', { name: 'Colour — Navy linen' })).toBeInTheDocument();
  });

  it('puts a dark tick on a light swatch and a light one on a dark swatch', () => {
    expect(isLightSwatch([{ color: '#eee5db' }])).toBe(true);
    expect(isLightSwatch([{ color: '#364358' }])).toBe(false);
    // Half black steel, half oak: the tick sits on the split, so the halves count together
    expect(isLightSwatch([{ color: '#1a1a1a' }, { color: '#cca883' }])).toBe(true);
    expect(isLightSwatch(undefined)).toBe(true);
  });

  it('lets the administrator add a finish to the product and take it off', async () => {
    const user = userEvent.setup();
    const Form = () => {
      const [chosen, setChosen] = useState(['natural-oak']);
      const toggle = (key: string) => setChosen((keys) => (keys.includes(key) ? keys.filter((k) => k !== key) : [...keys, key]));
      return <FinishPicker finishes={finishes} chosen={chosen} onToggle={toggle} />;
    };
    renderWithStore(<Form />);

    const oak = screen.getByRole('button', { name: 'Natural oak' });
    const navy = screen.getByRole('button', { name: 'Navy linen' });
    expect(oak).toHaveAttribute('aria-pressed', 'true');
    await user.click(navy);
    await user.click(oak);

    expect(navy).toHaveAttribute('aria-pressed', 'true');
    expect(oak).toHaveAttribute('aria-pressed', 'false');
  });
});
