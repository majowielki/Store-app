import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { describe, expect, it } from 'vitest';
import { product, productDetail } from '@/test/fixtures';
import { api, json, page } from '@/test/handlers';
import { renderWithStore } from '@/test/render';
import { server } from '@/test/server';
import ProductGallery from './ProductGallery';

const pineChair = product({ id: 8, title: 'Pine Chair', slug: 'pine-chair', salePrice: null, effectivePrice: 90 });

const current = () => screen.getByRole('button', { current: true });

describe('ProductGallery', () => {
  it('shows the room picture first, then the gallery, with a thumbnail for each', () => {
    renderWithStore(<ProductGallery product={productDetail()} />);

    const gallery = screen.getByRole('region', { name: 'Pictures of the Oak Table' });
    expect(within(gallery).getByRole('img', { name: 'Oak Table' })).toHaveAttribute('src', 'https://images.example.com/oak-table.jpg');
    expect(within(gallery).getByRole('img', { name: 'The oak grain up close' })).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /^Show picture/ })).toHaveLength(3);
    expect(current()).toHaveAccessibleName('Show picture 1 of 3');
  });

  it('moves between the pictures with the arrow keys, round the end too', async () => {
    const user = userEvent.setup();
    renderWithStore(<ProductGallery product={productDetail()} />);

    await user.click(screen.getByRole('button', { name: 'Show picture 2 of 3' }));
    expect(current()).toHaveAccessibleName('Show picture 2 of 3');
    await user.keyboard('{ArrowRight}');
    expect(current()).toHaveAccessibleName('Show picture 3 of 3');
    await user.keyboard('{ArrowRight}');
    expect(current()).toHaveAccessibleName('Show picture 1 of 3');
    await user.keyboard('{ArrowLeft}');
    expect(current()).toHaveAccessibleName('Show picture 3 of 3');
  });

  it('puts a point on the products of the room the shop lists, and none on the others', async () => {
    const queries: string[] = [];
    server.use(
      http.get(api('/products'), ({ request }) => {
        queries.push(new URL(request.url).search);
        // The lamp is not in the catalogue (retired, or not added yet)
        return json(page([pineChair]));
      }),
    );
    renderWithStore(<ProductGallery product={productDetail()} />);

    expect(await screen.findByRole('button', { name: 'Pine Chair, $90.00' })).toBeInTheDocument();
    expect(screen.getAllByRole('button', { expanded: false })).toHaveLength(1);
    expect(queries).toEqual(['?slugs=pine-chair%2Cretired-lamp&pageSize=100']);
  });

  it('keeps a product without a gallery to its one picture', () => {
    renderWithStore(<ProductGallery product={productDetail({ images: [], hotspots: [] })} />);

    expect(screen.queryByRole('button', { name: /^Show picture/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Next picture' })).not.toBeInTheDocument();
  });

  it('opens the picture in view larger and closes it with Escape', async () => {
    const user = userEvent.setup();
    renderWithStore(<ProductGallery product={productDetail({ hotspots: [] })} />);

    await user.click(screen.getByRole('button', { name: 'Show picture 3 of 3' }));
    await user.click(screen.getByRole('button', { name: 'View larger' }));
    const dialog = screen.getByRole('dialog', { name: 'Oak Table, picture 3 of 3' });
    expect(within(dialog).getByRole('img', { name: 'The Oak Table on its own' })).toBeInTheDocument();

    await user.keyboard('{ArrowLeft}');
    expect(screen.getByRole('dialog', { name: 'Oak Table, picture 2 of 3' })).toBeInTheDocument();
    await user.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});
