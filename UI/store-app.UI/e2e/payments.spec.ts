import { expect, test } from '@playwright/test';
import { addToCart, findProducts, pay, payWith, placeOrder, register, uniqueEmail } from './helpers';

/**
 * Paying for an order with the test cards: the card goes to the payment service, the order
 * service hears the outcome by a signed webhook and the saga marks the order paid.
 */
test.describe('payments', () => {
  test.beforeEach(async ({ page, request }) => {
    const [product] = await findProducts(request, { pageSize: '1' });
    await register(page, uniqueEmail('pay'));
    await addToCart(page, product.id);
  });

  test('17. card 4242 pays and the order shows the card that paid', async ({ page }) => {
    const orderId = await placeOrder(page);
    await expect(page.getByRole('timer')).toContainText(/Your pieces are held for 1[45]:\d\d/);

    await pay(page, '4242 4242 4242 4242');

    await expect(page.getByText(/paid with Visa •••• 4242/)).toBeVisible();
    await page.goto(`/orders/${orderId}`);
    await expect(page.getByRole('list', { name: 'Order progress' }).getByRole('listitem').nth(2)).toContainText('Visa •••• 4242');
    await page.goto('/orders');
    await expect(page.getByRole('row').nth(1)).toContainText('Paid');
  });

  test('18. card 0002 is declined and another card pays the same order', async ({ page }) => {
    const orderId = await placeOrder(page);

    await payWith(page, '4000 0000 0000 0002');
    await expect(page.getByRole('alert')).toHaveText('Your card was declined. Try another card.');
    // Still the same order, still waiting for its payment
    await expect(page).toHaveURL(new RegExp(`/orders/${orderId}/pay$`));

    await pay(page, '4242 4242 4242 4242');
  });

  test('19. card 3220 asks for 3-D Secure and the approval pays', async ({ page }) => {
    await placeOrder(page);

    await payWith(page, '4000 0000 0000 3220');
    const bank = page.getByRole('dialog', { name: 'Is this you?' });
    await expect(bank).toContainText('Visa •••• 3220');
    await bank.getByRole('button', { name: 'Approve' }).click();

    await expect(page.getByText('Payment received', { exact: true })).toBeVisible({ timeout: 20_000 });
  });
});
