import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import Compare from '@/pages/Compare';
import { product } from '@/test/fixtures';
import { api, json, page } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import CompareToggle from './CompareToggle';

describe('comparing products', () => {
  it('takes up to four pieces and says why it refuses a fifth', async () => {
    const { store } = renderWithStore(<CompareToggle product={{ id: 5, title: 'Oak Table' }} />, {
      preloadedState: { compare: { productIds: [1, 2, 3, 4] } },
    });

    await userEvent.click(screen.getByRole('button', { name: 'Compare Oak Table' }));

    expect(await screen.findByText('You can compare up to 4 pieces. Remove one to add Oak Table.', { exact: true })).toBeInTheDocument();
    expect(store.getState().compare.productIds).toEqual([1, 2, 3, 4]);
  });

  it('adds and removes a piece', async () => {
    const { store } = renderWithStore(<CompareToggle product={{ id: 5, title: 'Oak Table' }} />);

    await userEvent.click(screen.getByRole('button', { name: 'Compare Oak Table' }));
    expect(screen.getByRole('button', { name: 'Remove Oak Table from the comparison' })).toHaveAttribute('aria-pressed', 'true');
    await userEvent.click(screen.getByRole('button', { name: 'Remove Oak Table from the comparison' }));

    expect(store.getState().compare.productIds).toEqual([]);
  });

  it('shows the picked pieces side by side, row by row', async () => {
    server.use(
      http.get(api('/products'), () =>
        json(
          page([
            product({ id: 7, widthCm: 180, weightKg: 42, materials: ['oak'] }),
            product({ id: 8, title: 'Pine Chair', salePrice: null, effectivePrice: 90, price: 90, widthCm: null, colors: ['white'] }),
          ]),
        ),
      ),
    );
    renderWithStore(<Compare />, { preloadedState: { compare: { productIds: [7, 8] } } });

    const width = await screen.findByRole('row', { name: /^Width/ });
    expect(width).toHaveTextContent('180 cm');
    expect(width).toHaveTextContent('—');
    expect(screen.getByRole('row', { name: /^Weight/ })).toHaveTextContent('42 kg');
    expect(screen.getByRole('row', { name: /^Colours/ })).toHaveTextContent('white');
    expect(screen.getAllByRole('columnheader').map((header) => header.textContent)).toEqual([expect.stringContaining('Oak Table'), expect.stringContaining('Pine Chair')]);
  });
});
