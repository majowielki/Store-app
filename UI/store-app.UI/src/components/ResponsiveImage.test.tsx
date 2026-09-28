import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { imageSizes, imageSources } from '@/lib/images';
import ResponsiveImage from './ResponsiveImage';

const picture = 'https://store.blob.core.windows.net/product-images/OakDeskChair-1.webp';

describe('imageSources', () => {
  it('offers the smaller copies of a picture of the shop, and the tiny one to show meanwhile', () => {
    expect(imageSources(picture)).toEqual({
      src: picture,
      srcSet: [
        'https://store.blob.core.windows.net/product-images/w400/OakDeskChair-1.webp 400w',
        'https://store.blob.core.windows.net/product-images/w800/OakDeskChair-1.webp 800w',
        'https://store.blob.core.windows.net/product-images/w1200/OakDeskChair-1.webp 1200w',
        `${picture} 1600w`,
      ].join(', '),
      placeholder: 'https://store.blob.core.windows.net/product-images/w32/OakDeskChair-1.webp',
    });
  });

  it('leaves a picture from elsewhere as it is', () => {
    expect(imageSources('https://images.example.com/oak-table.jpg')).toEqual({ src: 'https://images.example.com/oak-table.jpg' });
    expect(imageSources('/assets/hero1.webp')).toEqual({ src: '/assets/hero1.webp' });
    expect(imageSources(`${picture}?v=2`)).toEqual({ src: `${picture}?v=2` });
  });
});

describe('ResponsiveImage', () => {
  it('lets the browser pick the copy for the width the picture is shown at', () => {
    render(<ResponsiveImage src={picture} size="card" placeholder alt="Oak desk chair" />);

    const image = screen.getByRole('img', { name: 'Oak desk chair' });
    expect(image).toHaveAttribute('srcset', imageSources(picture).srcSet);
    expect(image).toHaveAttribute('sizes', imageSizes.card);
    expect(image.style.backgroundImage).toContain('/w32/OakDeskChair-1.webp');
  });

  it('falls back to the picture itself when its copies are missing', () => {
    render(<ResponsiveImage src={picture} size="thumbnail" alt="Oak desk chair" />);
    const image = screen.getByRole('img', { name: 'Oak desk chair' });

    fireEvent.error(image);

    expect(image).not.toHaveAttribute('srcset');
    expect(image).not.toHaveAttribute('sizes');
    expect(image).toHaveAttribute('src', picture);
  });
});
