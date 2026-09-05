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

test('removes a stopped upload before a new upload starts', async ({ page }) => {
  await signIn(page);
  await page.route(/\/api\/uploads\/[^/]+\/chunks\/\d+$/, async route => {
    await new Promise(resolve => setTimeout(resolve, 1000));
    await route.continue();
  });

  await page.locator('#file-input').setInputFiles({
    name: 'stop-proof.txt',
    mimeType: 'text/plain',
    buffer: Buffer.alloc(128 * 1024, 'x')
  });

  const upload = page.locator('.upload-item').filter({ hasText: 'stop-proof.txt' });
  await expect(upload.getByRole('button', { name: 'Stop', exact: true })).toBeVisible();
  await upload.getByRole('button', { name: 'Stop', exact: true }).click();
  await expect(upload).toHaveCount(0);

  await page.locator('#file-input').setInputFiles({
    name: 'next-upload.txt',
    mimeType: 'text/plain',
    buffer: Buffer.alloc(128 * 1024, 'y')
  });
  await expect(page.locator('.upload-item').filter({ hasText: 'next-upload.txt' })).toBeVisible();
  await expect(upload).toHaveCount(0);
});

test('resumes a persisted upload when the same file is selected after reload', async ({ page }) => {
  await signIn(page);
  const fileName = `resume-${Date.now()}.bin`;
  const buffer = Buffer.alloc(8 * 1024 * 1024 + 1, 'r');
  const startResponse = await page.request.post('/api/uploads', {
    headers: { 'X-Upload-Token': token, 'X-File-Name': encodeURIComponent(fileName), 'X-File-Size': String(buffer.length), 'X-Target-Folder': '' }
  });
  const start = await startResponse.json();
  await page.request.put(`/api/uploads/${start.uploadId}/chunks/0`, {
    headers: { 'X-Upload-Token': token, 'Content-Type': 'application/octet-stream' },
    data: buffer.subarray(0, 8 * 1024 * 1024)
  });
  await page.evaluate(record => localStorage.setItem('file-workspace-upload-resume', JSON.stringify([record])), {
    uploadId: start.uploadId, name: fileName, size: buffer.length, targetPath: ''
  });

  await page.reload();
  const resumeRequest = page.waitForRequest(request => request.url().endsWith(`/api/uploads/${start.uploadId}/resume`));
  await page.locator('#file-input').setInputFiles({ name: fileName, mimeType: 'application/octet-stream', buffer });
  const request = await resumeRequest;

  expect(request.headers()['x-upload-token']).toBe(token);
  await expect(page.getByRole('button', { name: fileName, exact: true })).toBeVisible();
  await expect(page.locator('#upload-panel')).toBeHidden();
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

test('creates a scoped GET ticket for an individual download', async ({ page }) => {
  await signIn(page);
  const fileName = `ticket-${Date.now()}.txt`;
  await page.locator('#file-input').setInputFiles({ name: fileName, mimeType: 'text/plain', buffer: Buffer.from('Ticket test') });
  await expect(page.getByRole('button', { name: fileName, exact: true })).toBeVisible();

  const ticketRequest = page.waitForRequest(request => request.url().endsWith('/api/files/download-tickets'));
  const downloadPopup = page.waitForEvent('popup');
  await page.locator('.file-entry').filter({ has: page.getByRole('button', { name: fileName, exact: true }) }).getByRole('button', { name: 'Download', exact: true }).click();
  const request = await ticketRequest;
  const popup = await downloadPopup;

  expect(request.method()).toBe('POST');
  expect(request.headers()['x-upload-token']).toBe(token);
  expect(request.postDataJSON()).toEqual({ path: fileName });
  expect(request.url()).not.toContain(token);
  await popup.close();
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

  await page.getByRole('button', { name: 'Close navigation' }).click();
  await expect(page.locator('.workspace-shell')).toHaveClass(/sidebar-collapsed/);
  expect(await page.locator('#sidebar').evaluate(element => element.getBoundingClientRect().width === 0)).toBeTruthy();
  await page.getByRole('button', { name: 'Open navigation' }).click();
  await expect(page.locator('.workspace-shell')).not.toHaveClass(/sidebar-collapsed/);
});
