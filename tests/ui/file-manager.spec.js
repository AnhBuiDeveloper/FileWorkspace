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

  for (const width of [320, 375, 430, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.getByRole('button', { name: 'Delete', exact: true }).click();
    await expect(page.locator('#delete-file-dialog')).toBeVisible();
    expect(await page.locator('body').evaluate(body => body.scrollWidth <= window.innerWidth)).toBeTruthy();
    expect(await page.locator('#delete-file-dialog').evaluate(dialog => {
      const bounds = dialog.getBoundingClientRect();
      return bounds.left >= 0 && bounds.right <= window.innerWidth && bounds.top >= 0 && bounds.bottom <= window.innerHeight;
    })).toBeTruthy();
    await page.getByRole('button', { name: 'Cancel', exact: true }).click();
  }

  await page.getByRole('button', { name: 'Delete', exact: true }).click();
  await expect(page.locator('#delete-file-dialog')).toBeVisible();
  await expect(page.locator('#delete-file-dialog-message')).toContainText(fileName);
  await page.getByRole('button', { name: 'Cancel', exact: true }).click();
  await expect(page.getByRole('button', { name: fileName, exact: true })).toBeVisible();

  await page.getByRole('button', { name: 'Delete', exact: true }).click();
  await page.getByRole('button', { name: 'Delete permanently', exact: true }).click();
  await expect(page.getByRole('button', { name: fileName, exact: true })).toBeHidden();
});

test('selects multiple files and folders for a ZIP download', async ({ page }) => {
  await signIn(page);
  const folderName = 'zip-' + Date.now();
  const fileName = 'zip-proof.txt';

  await page.getByRole('button', { name: 'New folder' }).click();
  await page.locator('#folder-name').fill(folderName);
  await page.getByRole('button', { name: 'Create folder', exact: true }).click();
  await page.locator('#file-input').setInputFiles({ name: fileName, mimeType: 'text/plain', buffer: Buffer.from('ZIP download test') });
  await expect(page.getByRole('button', { name: fileName, exact: true })).toBeVisible();

  await page.getByLabel(`Select ${folderName}`).check();
  await page.getByLabel(`Select ${fileName}`).check();
  await expect(page.getByRole('button', { name: `Download ZIP (2)`, exact: true })).toBeEnabled();
  await expect(page.getByRole('button', { name: 'Delete (2)', exact: true })).toBeEnabled();
  for (const width of [320, 375, 430, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await expect(page.getByRole('button', { name: `Download ZIP (2)`, exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Delete (2)', exact: true })).toBeVisible();
    expect(await page.locator('body').evaluate(body => body.scrollWidth <= window.innerWidth)).toBeTruthy();
  }
  await page.getByRole('button', { name: `Download ZIP (2)`, exact: true }).click();
  await expect(page.getByRole('status')).toHaveText('ZIP download has started.');
  await page.getByRole('button', { name: 'Delete (2)', exact: true }).click();
  await expect(page.locator('#delete-file-dialog-message')).toContainText('all their contents');
  await page.getByRole('button', { name: 'Delete permanently', exact: true }).click();
  await expect(page.getByRole('button', { name: folderName, exact: true })).toBeHidden();
  await expect(page.getByRole('button', { name: fileName, exact: true })).toBeHidden();
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
