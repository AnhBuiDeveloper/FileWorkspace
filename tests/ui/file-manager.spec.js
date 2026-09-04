import { expect, test } from '@playwright/test';

const token = 'playwright-e2e-token';

async function signIn(page) {
  await page.goto('/');
  await page.selectOption('#language-select', 'en');
  await page.locator('#upload-token').fill(token);
  await page.getByRole('button', { name: 'Refresh' }).click();
  await expect(page.getByRole('heading', { name: 'Your files' })).toBeVisible();
}

test('remembers the token, switches language, and logs out', async ({ page }) => {
  await signIn(page);
  await page.reload();

  await expect(page.locator('#upload-token')).toHaveValue(token);
  await expect(page.getByRole('heading', { name: 'Your files' })).toBeVisible();
  await page.selectOption('#language-select', 'vi');
  await expect(page.getByRole('heading', { name: 'File của bạn' })).toBeVisible();
  await page.getByRole('button', { name: 'Đăng xuất' }).click();
  await expect(page.locator('#files-panel')).toBeHidden();
  await expect(page.locator('#upload-token')).toHaveValue('');
});

test('creates a folder and uploads a file into it', async ({ page }) => {
  await signIn(page);
  const folderName = 'e2e-' + Date.now();
  const fileName = 'playwright-proof.txt';

  await page.getByRole('button', { name: 'New folder' }).click();
  await page.locator('#folder-name').fill(folderName);
  await page.getByRole('button', { name: 'Create folder', exact: true }).click();
  await expect(page.getByRole('button', { name: folderName, exact: true })).toBeVisible();
  await page.getByRole('button', { name: folderName, exact: true }).click();
  await expect(page.locator('#breadcrumbs').getByRole('button', { name: folderName, exact: true })).toBeDisabled();
  await page.locator('#file-input').setInputFiles({
    name: fileName,
    mimeType: 'text/plain',
    buffer: Buffer.from('Playwright verifies the upload flow.')
  });

  await expect(page.getByRole('button', { name: fileName, exact: true })).toBeVisible();
  await expect(page.locator('.new-badge')).toHaveText('New');
  await expect(page.locator('#upload-panel')).toBeHidden();
});

test('renders the Explorer tree and keeps the compact layout within the viewport', async ({ page }) => {
  await signIn(page);
  const folderName = 'tree-' + Date.now();

  await page.getByRole('button', { name: 'New folder' }).click();
  await page.locator('#folder-name').fill(folderName);
  await page.getByRole('button', { name: 'Create folder', exact: true }).click();
  await expect(page.getByLabel(`Folder: ${folderName}`, { exact: true })).toBeVisible();

  await page.setViewportSize({ width: 320, height: 700 });
  await page.getByRole('button', { name: 'Open navigation' }).click();
  await expect(page.locator('#sidebar')).toHaveClass(/is-open/);
  expect(await page.locator('body').evaluate(body => body.scrollWidth <= window.innerWidth)).toBeTruthy();
});

test('keeps the file workspace in the main grid column on desktop', async ({ page }) => {
  await signIn(page);
  await page.setViewportSize({ width: 1440, height: 900 });

  expect(await page.locator('.workspace-main').evaluate(element => {
    const main = element.getBoundingClientRect();
    const sidebar = document.querySelector('#sidebar').getBoundingClientRect();
    return main.left >= sidebar.right && main.width > 900;
  })).toBeTruthy();
});
