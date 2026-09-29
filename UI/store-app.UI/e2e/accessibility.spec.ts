import { expect, test, type Page } from '@playwright/test';
import { expectNoSeriousViolations, tabTo } from './a11y';
import { API, addToCart, expectToast, findProducts, password, register, uniqueEmail } from './helpers';

/** The page's content, where the skip link and every change of page put the focus. */
const main = (page: Page) => page.locator('main#main-content');

const pageTitle = (page: Page) => page.getByRole('heading', { level: 1 }).first();

test.describe('accessibility', () => {
  test('23. the key pages have no serious or critical accessibility problems', async ({ page, request }) => {
    // Sections fade in when scrolled to; without motion they stand as they will look, which is what is checked
    await page.emulateMedia({ reducedMotion: 'reduce' });
    const [product] = await findProducts(request, { pageSize: '1' });

    const open = async (name: string, address: string) => {
      await page.goto(address);
      await expect(pageTitle(page)).toBeVisible();
      await expectNoSeriousViolations(page, name);
    };

    await open('the home page', '/');
    await open('the catalogue', '/products');
    await open('a product page', `/products/${product.id}`);
    await open('a journal article', '/journal/caring-for-oak');
    await open('the About page', '/about');
    await open('the sign-in page', '/login');

    // The bag and the checkout, with something in them
    await register(page, uniqueEmail('a11y'));
    await addToCart(page, product.id);
    await open('the bag', '/cart');
    await open('the checkout', '/checkout');
  });

  test('24. the whole purchase works from the keyboard alone', async ({ page, request }) => {
    const [product] = await findProducts(request, { pageSize: '1' });
    const email = uniqueEmail('keys');
    const account = await request.post(`${API}/auth/register`, { data: { email, password, confirmPassword: password, firstName: 'Keyboard', lastName: 'Tester' } });
    expect(account.ok()).toBeTruthy();

    // Signing in: Tab to the fields, Enter sends the form
    await page.goto('/login');
    await tabTo(page, page.getByLabel('email', { exact: true }));
    await page.keyboard.type(email);
    await page.keyboard.press('Tab');
    await page.keyboard.type(password);
    await page.keyboard.press('Enter');
    await expectToast(page, 'Successfully logged in!');
    await expect(page).toHaveURL(/\/$/);

    // The first Tab of a page offers to skip the header
    await page.goto('/');
    await expect(pageTitle(page)).toBeVisible();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Skip to content' })).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(main(page)).toBeFocused();

    // Ctrl+K searches; the product found opens with the focus on its page, not left in the header
    await page.keyboard.press('Control+k');
    await expect(page.getByRole('searchbox', { name: 'Search products' })).toBeFocused();
    await page.keyboard.type(product.title);
    const result = page.getByRole('dialog').getByRole('link').filter({ has: page.getByText(product.title, { exact: true }) });
    await tabTo(page, result);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(new RegExp(`/products/${product.id}$`));
    await expect(pageTitle(page)).toHaveText(product.title);
    await expect(main(page)).toBeFocused();

    // Into the bag, and from the bag that slides in straight to the checkout
    await tabTo(page, page.getByRole('button', { name: 'Add to bag' }));
    await page.keyboard.press('Enter');
    const bag = page.getByRole('dialog', { name: 'Added to your bag' });
    await expect(bag).toBeVisible();
    await tabTo(page, bag.getByRole('link', { name: 'Checkout' }));
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/checkout$/);
    await expect(bag).toBeHidden();
    await expect(main(page)).toBeFocused();

    await tabTo(page, page.getByLabel('address', { exact: true }));
    await page.keyboard.type('Keyboard Street 1');
    await page.keyboard.press('Enter');
    await expectToast(page, 'Order placed');
    await expect(page).toHaveURL(/\/orders\/\d+\/pay$/);

    // The card form, once the pieces are held; checked for accessibility problems on the way
    await tabTo(page, page.getByLabel('Card number'));
    await expectNoSeriousViolations(page, 'the payment page');
    await page.keyboard.type('4242424242424242');
    await page.keyboard.press('Tab');
    await page.keyboard.type('1240');
    await page.keyboard.press('Tab');
    await page.keyboard.type('123');
    await page.keyboard.press('Enter');
    await expect(page.getByText('Payment received', { exact: true })).toBeVisible({ timeout: 20_000 });
    await expectNoSeriousViolations(page, 'the confirmation of the purchase');
  });
});
