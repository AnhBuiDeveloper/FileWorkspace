const CHUNK_SIZE = 8 * 1024 * 1024;
const MAX_CONCURRENT_UPLOADS = 3;

const copy = {
  vi: {
    documentTitle: 'File Manager', languageLabel: 'Ngôn ngữ', title: 'Không gian file riêng', hint: 'Quản lý folder, upload nhiều file và tải xuống an toàn với upload token.',
    tokenLabel: 'Upload token', tokenPlaceholder: 'Nhập upload token', logout: 'Đăng xuất', loggedOut: 'Đã đăng xuất và dừng các upload đang hoạt động.', tokenRequired: 'Nhập upload token trước.',
    fileManagerKicker: 'FILE MANAGER', filesTitle: 'File của bạn', newFolder: 'Tạo folder', uploadFiles: 'Upload file', uploadFolder: 'Upload folder', refreshFiles: 'Làm mới',
    dropZoneText: 'Kéo thả file vào đây để upload vào folder hiện tại', dropZoneLabel: 'Chọn file để upload vào folder hiện tại', home: 'Upload', filesLoading: 'Đang tải nội dung…', filesEmpty: 'Folder này đang trống.', refreshFilesHint: 'Nhấn Làm mới để xem file với token hiện tại.',
    folder: 'Folder', download: 'Tải xuống', downloading: 'Đang chuẩn bị…', downloadStarted: 'Đã bắt đầu tải file.', uploadListTitle: 'Tiến trình upload',
    folderName: 'Tên folder', cancel: 'Hủy', createFolder: 'Tạo folder', creatingFolder: 'Đang tạo…', folderCreated: 'Đã tạo folder.',
    states: { preparing: 'Đang chuẩn bị…', queued: 'Đang chờ…', uploading: 'Đang upload…', paused: 'Đã tạm dừng', stopped: 'Đã dừng', completed: 'Hoàn tất', error: 'Có lỗi' },
    buttons: { pause: 'Pause', resume: 'Resume', stop: 'Stop' },
    errors: { createSession: 'Không thể tạo phiên upload.', invalidResponse: 'Phản hồi server không hợp lệ.', connection: 'Không kết nối được server.', uploadFailed: 'Upload thất bại.', unauthorized: 'Upload token không hợp lệ hoặc đã hết quyền truy cập.', invalidFileName: 'Tên file không hợp lệ.', invalidFileSize: 'Dung lượng file không hợp lệ.', invalidFolder: 'Tên hoặc đường dẫn thư mục không hợp lệ.', folderExists: 'Tên thư mục đã tồn tại.', folderNotFound: 'Thư mục không tồn tại.', sessionNotFound: 'Phiên upload không tồn tại hoặc đã kết thúc.', invalidChunk: 'Chunk không hợp lệ.', invalidChunkSize: 'Kích thước chunk không hợp lệ.', incompleteChunk: 'Dữ liệu chunk chưa hoàn chỉnh.', downloadFailed: 'Không thể tải file.' }
  },
  en: {
    documentTitle: 'File Manager', languageLabel: 'Language', title: 'Private file workspace', hint: 'Manage folders, upload multiple files, and download securely with an upload token.',
    tokenLabel: 'Upload token', tokenPlaceholder: 'Enter upload token', logout: 'Log out', loggedOut: 'You have been logged out and active uploads have been stopped.', tokenRequired: 'Enter the upload token first.',
    fileManagerKicker: 'FILE MANAGER', filesTitle: 'Your files', newFolder: 'New folder', uploadFiles: 'Upload files', uploadFolder: 'Upload folder', refreshFiles: 'Refresh',
    dropZoneText: 'Drop files here to upload them to the current folder', dropZoneLabel: 'Choose files to upload to the current folder', home: 'Upload', filesLoading: 'Loading contents…', filesEmpty: 'This folder is empty.', refreshFilesHint: 'Select Refresh to view files with the current token.',
    folder: 'Folder', download: 'Download', downloading: 'Preparing…', downloadStarted: 'The download has started.', uploadListTitle: 'Upload activity',
    folderName: 'Folder name', cancel: 'Cancel', createFolder: 'Create folder', creatingFolder: 'Creating…', folderCreated: 'Folder created.',
    states: { preparing: 'Preparing…', queued: 'Queued', uploading: 'Uploading…', paused: 'Paused', stopped: 'Stopped', completed: 'Completed', error: 'Error' },
    buttons: { pause: 'Pause', resume: 'Resume', stop: 'Stop' },
    errors: { createSession: 'Could not create the upload session.', invalidResponse: 'The server returned an invalid response.', connection: 'Could not connect to the server.', uploadFailed: 'Upload failed.', unauthorized: 'The upload token is invalid or no longer has access.', invalidFileName: 'The file name is invalid.', invalidFileSize: 'The file size is invalid.', invalidFolder: 'The folder name or path is invalid.', folderExists: 'A folder with this name already exists.', folderNotFound: 'The folder does not exist.', sessionNotFound: 'The upload session does not exist or has ended.', invalidChunk: 'The upload chunk is invalid.', invalidChunkSize: 'The upload chunk size is invalid.', incompleteChunk: 'The upload chunk is incomplete.', downloadFailed: 'Could not download the file.' }
  }
};

const fileInput = document.querySelector('#file-input');
const folderInput = document.querySelector('#folder-input');
const tokenInput = document.querySelector('#upload-token');
const languageSelect = document.querySelector('#language-select');
const logoutButton = document.querySelector('#logout-button');
const filesPanel = document.querySelector('#files-panel');
const filesList = document.querySelector('#files-list');
const filesStatus = document.querySelector('#files-status');
const refreshFilesButton = document.querySelector('#refresh-files');
const newFolderButton = document.querySelector('#new-folder');
const uploadFilesButton = document.querySelector('#upload-files');
const uploadFolderButton = document.querySelector('#upload-folder');
const dropZone = document.querySelector('#drop-zone');
const breadcrumbs = document.querySelector('#breadcrumbs');
const uploadPanel = document.querySelector('#upload-panel');
const uploadList = document.querySelector('#upload-list');
const uploadCount = document.querySelector('#upload-count');
const status = document.querySelector('#status');
const folderDialog = document.querySelector('#create-folder-dialog');
const folderForm = document.querySelector('#create-folder-form');
const folderNameInput = document.querySelector('#folder-name');
const folderDialogStatus = document.querySelector('#folder-dialog-status');
const closeFolderDialogButton = document.querySelector('#close-folder-dialog');
const cancelFolderDialogButton = document.querySelector('#cancel-folder-dialog');

const uploads = [];
let activeTransfers = 0;
let currentPath = '';
let listedToken = '';
let entries = [];
let fileRequestVersion = 0;
let currentLanguage = localStorage.getItem('file-upload-language') || (navigator.language.startsWith('vi') ? 'vi' : 'en');

tokenInput.value = localStorage.getItem('upload-token') || '';
languageSelect.value = currentLanguage;
applyLanguage();
updateAuthControls();
if (tokenInput.value.trim()) loadFiles();

tokenInput.addEventListener('input', () => {
  const token = tokenInput.value.trim();
  if (token) localStorage.setItem('upload-token', token);
  else localStorage.removeItem('upload-token');
  updateAuthControls();
});
languageSelect.addEventListener('change', () => {
  currentLanguage = languageSelect.value;
  localStorage.setItem('file-upload-language', currentLanguage);
  applyLanguage();
});
logoutButton.addEventListener('click', logout);
refreshFilesButton.addEventListener('click', () => loadFiles());
uploadFilesButton.addEventListener('click', () => fileInput.click());
uploadFolderButton.addEventListener('click', () => folderInput.click());
newFolderButton.addEventListener('click', openFolderDialog);
closeFolderDialogButton.addEventListener('click', () => folderDialog.close());
cancelFolderDialogButton.addEventListener('click', () => folderDialog.close());
folderForm.addEventListener('submit', createFolder);
fileInput.addEventListener('change', () => { addFiles(fileInput.files, false); fileInput.value = ''; });
folderInput.addEventListener('change', () => { addFiles(folderInput.files, true); folderInput.value = ''; });

['dragenter', 'dragover'].forEach(event => dropZone.addEventListener(event, e => {
  e.preventDefault();
  dropZone.classList.add('dragging');
}));
['dragleave', 'drop'].forEach(event => dropZone.addEventListener(event, e => {
  e.preventDefault();
  dropZone.classList.remove('dragging');
}));
dropZone.addEventListener('drop', e => addFiles(e.dataTransfer.files, false));
dropZone.addEventListener('click', () => fileInput.click());
dropZone.addEventListener('keydown', e => {
  if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); fileInput.click(); }
});

function addFiles(fileList, preserveFolderStructure) {
  const files = [...fileList].filter(file => file instanceof File);
  const token = tokenInput.value.trim();
  if (!files.length) return;
  if (!token) { setStatus(t('tokenRequired'), 'error'); tokenInput.focus(); return; }

  setStatus('', '');
  files.forEach(file => {
    const localPath = preserveFolderStructure ? file.webkitRelativePath : '';
    const localFolder = localPath.includes('/') ? localPath.split('/').slice(0, -1).join('/') : '';
    const destinationPath = joinPath(currentPath, localFolder);
    const upload = new UploadTask(file, token, destinationPath);
    uploads.push(upload);
    upload.render();
    upload.initialize();
  });
  updateUploadCount();
}

function scheduleUploads() {
  while (activeTransfers < MAX_CONCURRENT_UPLOADS) {
    const next = uploads.find(upload => upload.state === 'queued' && upload.uploadId);
    if (!next) break;
    next.transfer();
  }
  updateUploadCount();
}

class UploadTask {
  constructor(file, token, targetPath) {
    this.file = file;
    this.token = token;
    this.targetPath = targetPath;
    this.uploadId = null;
    this.state = 'preparing';
    this.uploadedBytes = 0;
    this.nextChunk = 0;
    this.xhr = null;
    this.speed = 0;
    this.error = '';
    this.element = null;
  }
  get totalChunks() { return Math.ceil(this.file.size / CHUNK_SIZE); }

  async initialize() {
    try {
      const response = await fetch('/api/uploads', { method: 'POST', headers: {
        'X-Upload-Token': this.token, 'X-File-Name': encodeURIComponent(this.file.name), 'X-File-Size': String(this.file.size), 'X-Target-Folder': encodeURIComponent(this.targetPath)
      }});
      const data = await readResponse(response);
      if (!response.ok) throw new Error(response.status === 401 ? t('errors.unauthorized') : data.error || t('errors.uploadFailed'));
      this.uploadId = data.uploadId;
      this.uploadedBytes = data.uploadedBytes || 0;
      if (this.state === 'stopped') { await this.deleteSession(); return; }
      if (data.completed) { this.state = 'completed'; this.uploadedBytes = this.file.size; loadFiles(); }
      else { this.state = this.state === 'paused' ? 'paused' : 'queued'; scheduleUploads(); }
    } catch (error) {
      if (this.state !== 'stopped') { this.state = 'error'; this.error = localizeError(error.message || t('errors.createSession')); }
    }
    this.render();
    updateUploadCount();
  }

  async transfer() {
    if (this.state !== 'queued') return;
    this.state = 'uploading'; activeTransfers += 1; this.render(); updateUploadCount();
    try {
      while (this.state === 'uploading' && this.nextChunk < this.totalChunks) {
        const result = await this.sendChunk(this.nextChunk);
        if (this.state !== 'uploading') break;
        this.uploadedBytes = result.uploadedBytes;
        this.nextChunk += 1;
        if (result.completed || this.nextChunk === this.totalChunks) {
          this.state = 'completed'; this.uploadedBytes = this.file.size; this.speed = 0; loadFiles();
        }
        this.render();
      }
    } catch (error) {
      if (!['paused', 'stopped'].includes(this.state)) { this.state = 'error'; this.error = localizeError(error.message || t('errors.uploadFailed')); }
    } finally {
      this.xhr = null; activeTransfers -= 1; this.render(); scheduleUploads();
    }
  }

  sendChunk(chunkIndex) {
    const start = chunkIndex * CHUNK_SIZE;
    const chunk = this.file.slice(start, Math.min(start + CHUNK_SIZE, this.file.size));
    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest(); this.xhr = xhr;
      let lastLoaded = 0; let lastTime = performance.now();
      xhr.open('PUT', `/api/uploads/${this.uploadId}/chunks/${chunkIndex}`);
      xhr.setRequestHeader('Content-Type', 'application/octet-stream'); xhr.setRequestHeader('X-Upload-Token', this.token);
      xhr.upload.onprogress = event => {
        if (!event.lengthComputable || this.state !== 'uploading') return;
        const now = performance.now(); this.speed = (event.loaded - lastLoaded) / Math.max((now - lastTime) / 1000, .001);
        lastLoaded = event.loaded; lastTime = now; this.render(this.uploadedBytes + event.loaded);
      };
      xhr.onload = () => {
        if (xhr.status >= 200 && xhr.status < 300) { try { resolve(JSON.parse(xhr.responseText)); } catch { reject(new Error(t('errors.invalidResponse'))); } return; }
        reject(new Error(xhr.status === 401 ? t('errors.unauthorized') : readXhrError(xhr) || t('errors.uploadFailed')));
      };
      xhr.onerror = () => reject(new Error(t('errors.connection')));
      xhr.onabort = () => reject(new DOMException(t('states.paused'), 'AbortError'));
      xhr.send(chunk);
    });
  }

  pause() { if (!['preparing', 'queued', 'uploading'].includes(this.state)) return; this.state = 'paused'; this.speed = 0; this.xhr?.abort(); this.render(); updateUploadCount(); }
  resume() { if (this.state !== 'paused') return; this.state = this.uploadId ? 'queued' : 'preparing'; this.error = ''; this.render(); scheduleUploads(); }
  async stop() { if (['stopped', 'completed'].includes(this.state)) return; this.state = 'stopped'; this.speed = 0; this.xhr?.abort(); this.render(); updateUploadCount(); await this.deleteSession(); }
  async deleteSession() { if (!this.uploadId) return; try { await fetch(`/api/uploads/${this.uploadId}`, { method: 'DELETE', headers: { 'X-Upload-Token': this.token } }); } catch { /* local state is already stopped */ } }

  render(displayedBytes = this.uploadedBytes) {
    if (!this.element) { this.element = document.createElement('article'); this.element.className = 'upload-item'; uploadList.append(this.element); }
    const percent = this.file.size ? Math.min(100, Math.round((displayedBytes / this.file.size) * 100)) : 100;
    this.element.className = `upload-item ${this.state}`; this.element.replaceChildren();
    const header = document.createElement('div'); header.className = 'file-line';
    const name = document.createElement('strong'); name.textContent = this.file.name; name.title = this.file.name;
    const state = document.createElement('span'); state.className = 'upload-state'; state.textContent = this.state === 'error' ? this.error : t(`states.${this.state}`); header.append(name, state);
    const destination = document.createElement('div'); destination.className = 'upload-destination'; destination.textContent = `/${this.targetPath || t('home')}`;
    const progress = document.createElement('div'); progress.className = 'bar'; progress.setAttribute('role', 'progressbar'); progress.setAttribute('aria-valuenow', String(percent)); progress.setAttribute('aria-valuemin', '0'); progress.setAttribute('aria-valuemax', '100'); const fill = document.createElement('div'); fill.style.width = `${percent}%`; progress.append(fill);
    const footer = document.createElement('div'); footer.className = 'upload-footer'; const stats = document.createElement('span'); stats.className = 'stats'; stats.textContent = `${formatBytes(displayedBytes)} / ${formatBytes(this.file.size)} · ${this.state === 'uploading' ? `${formatBytes(this.speed)}/s` : `${percent}%`}`;
    const controls = document.createElement('div'); controls.className = 'upload-controls';
    if (['preparing', 'queued', 'uploading'].includes(this.state)) { controls.append(button(t('buttons.pause'), 'secondary', () => this.pause()), button(t('buttons.stop'), 'danger', () => this.stop())); }
    else if (this.state === 'paused') { controls.append(button(t('buttons.resume'), 'primary', () => this.resume()), button(t('buttons.stop'), 'danger', () => this.stop())); }
    else if (this.state === 'error') controls.append(button(t('buttons.stop'), 'danger', () => this.stop()));
    footer.append(stats, controls); this.element.append(header, destination, progress, footer);
  }
}

async function loadFiles(path = currentPath) {
  const token = tokenInput.value.trim();
  if (!token) return;
  const requestVersion = ++fileRequestVersion;
  refreshFilesButton.disabled = true; filesStatus.textContent = t('filesLoading');
  try {
    const response = await fetch(`/api/files?path=${encodeURIComponent(path)}`, { headers: { 'X-Upload-Token': token } });
    const data = await readResponse(response);
    if (!response.ok) throw new Error(response.status === 401 ? t('errors.unauthorized') : data.error || t('errors.connection'));
    if (requestVersion !== fileRequestVersion || token !== tokenInput.value.trim()) return;
    currentPath = data.path || ''; entries = Array.isArray(data.entries) ? data.entries : []; listedToken = token; renderFileManager();
  } catch (error) {
    if (requestVersion !== fileRequestVersion) return;
    entries = []; filesList.replaceChildren(); filesStatus.textContent = localizeError(error.message || t('errors.connection'));
  } finally { if (requestVersion === fileRequestVersion) refreshFilesButton.disabled = false; }
}

function renderFileManager() {
  renderBreadcrumbs(); filesList.replaceChildren();
  if (!entries.length) { filesStatus.textContent = t('filesEmpty'); return; }
  filesStatus.textContent = `${entries.length} ${currentLanguage === 'vi' ? 'mục' : entries.length === 1 ? 'item' : 'items'}`;
  entries.forEach(entry => {
    const row = document.createElement('article'); row.className = `file-entry ${entry.type}`; row.setAttribute('role', 'listitem');
    const icon = document.createElement('span'); icon.className = 'entry-icon'; icon.textContent = entry.type === 'folder' ? '▰' : '▱';
    const info = document.createElement('div'); info.className = 'entry-info';
    const name = document.createElement('button'); name.className = 'entry-name'; name.type = 'button'; name.textContent = entry.name; name.title = entry.name;
    if (entry.type === 'folder') name.addEventListener('click', () => loadFiles(joinPath(currentPath, entry.name)));
    else name.addEventListener('click', () => downloadFile(entry));
    const details = document.createElement('div'); details.className = 'entry-details'; details.textContent = `${entry.type === 'folder' ? t('folder') : formatBytes(entry.bytes)} · ${formatDate(entry.modifiedAtUtc)}`;
    info.append(name, details);
    const action = entry.type === 'folder'
      ? button('›', 'open-folder', () => loadFiles(joinPath(currentPath, entry.name)), entry.name)
      : button(t('download'), 'download', () => downloadFile(entry));
    row.append(icon, info, action); filesList.append(row);
  });
}

function renderBreadcrumbs() {
  breadcrumbs.replaceChildren();
  const parts = currentPath ? currentPath.split('/') : [];
  const crumbs = [{ name: t('home'), path: '' }, ...parts.map((name, index) => ({ name, path: parts.slice(0, index + 1).join('/') }))];
  crumbs.forEach((crumb, index) => {
    if (index) { const separator = document.createElement('span'); separator.className = 'breadcrumb-separator'; separator.textContent = '/'; breadcrumbs.append(separator); }
    const item = document.createElement('button'); item.type = 'button'; item.textContent = crumb.name; item.className = 'breadcrumb'; item.disabled = crumb.path === currentPath; item.addEventListener('click', () => loadFiles(crumb.path)); breadcrumbs.append(item);
  });
}

function openFolderDialog() {
  if (!tokenInput.value.trim()) { setStatus(t('tokenRequired'), 'error'); tokenInput.focus(); return; }
  folderForm.reset(); folderDialogStatus.textContent = '';
  folderDialog.showModal(); folderNameInput.focus();
}

async function createFolder(event) {
  event.preventDefault();
  const token = tokenInput.value.trim(); const name = folderNameInput.value.trim();
  if (!name || !token) return;
  const submit = folderForm.querySelector('[type="submit"]'); submit.disabled = true; folderDialogStatus.textContent = t('creatingFolder');
  try {
    const response = await fetch('/api/folders', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Upload-Token': token }, body: JSON.stringify({ parentPath: currentPath, name }) });
    const data = await readResponse(response);
    if (!response.ok) throw new Error(response.status === 401 ? t('errors.unauthorized') : data.error || t('errors.invalidFolder'));
    folderDialog.close(); setStatus(t('folderCreated'), 'success'); loadFiles();
  } catch (error) { folderDialogStatus.textContent = localizeError(error.message || t('errors.invalidFolder')); }
  finally { submit.disabled = false; }
}

function downloadFile(entry) {
  const token = tokenInput.value.trim(); if (!token) return;
  const form = document.createElement('form'); form.method = 'post'; form.action = '/api/files/download'; form.target = '_blank'; form.hidden = true;
  [['path', joinPath(currentPath, entry.name)], ['token', token]].forEach(([name, value]) => { const input = document.createElement('input'); input.type = 'hidden'; input.name = name; input.value = value; form.append(input); });
  document.body.append(form); form.submit(); form.remove(); filesStatus.textContent = t('downloadStarted');
}

function logout() {
  uploads.filter(upload => !['completed', 'stopped', 'error'].includes(upload.state)).forEach(upload => upload.stop());
  localStorage.removeItem('upload-token'); tokenInput.value = ''; entries = []; listedToken = ''; currentPath = ''; updateAuthControls(); setStatus(t('loggedOut'), 'success'); tokenInput.focus();
}

function updateAuthControls() {
  const token = tokenInput.value.trim(); logoutButton.hidden = !token; filesPanel.hidden = !token;
  if (!token) { filesList.replaceChildren(); filesStatus.textContent = ''; breadcrumbs.replaceChildren(); return; }
  if (listedToken !== token) { entries = []; filesList.replaceChildren(); filesStatus.textContent = t('refreshFilesHint'); }
}

function updateUploadCount() {
  const active = uploads.filter(upload => ['preparing', 'queued', 'uploading', 'paused'].includes(upload.state)).length;
  uploadPanel.hidden = uploads.length === 0; const count = active || uploads.length; uploadCount.textContent = currentLanguage === 'vi' ? `${count} file` : `${count} file${count === 1 ? '' : 's'}`;
}

function applyLanguage() {
  document.documentElement.lang = currentLanguage; document.title = t('documentTitle');
  document.querySelectorAll('[data-i18n]').forEach(element => { element.textContent = t(element.dataset.i18n); });
  document.querySelectorAll('[data-i18n-placeholder]').forEach(element => { element.placeholder = t(element.dataset.i18nPlaceholder); });
  document.querySelectorAll('[data-i18n-title]').forEach(element => { element.title = t(element.dataset.i18nTitle); });
  document.querySelectorAll('[data-i18n-aria-label]').forEach(element => { element.setAttribute('aria-label', t(element.dataset.i18nAriaLabel)); });
  languageSelect.setAttribute('aria-label', t('languageLabel')); filesPanel.setAttribute('aria-label', t('filesTitle')); uploadPanel.setAttribute('aria-label', t('uploadListTitle'));
  uploads.forEach(upload => upload.render()); renderFileManager(); updateUploadCount();
}

function button(label, className, onClick, title = '') { const element = document.createElement('button'); element.type = 'button'; element.className = `button ${className}`; element.textContent = label; if (title) element.title = title; element.addEventListener('click', onClick); return element; }
async function readResponse(response) { try { return await response.json(); } catch { return {}; } }
function readXhrError(xhr) { try { return JSON.parse(xhr.responseText).error; } catch { return ''; } }
function setStatus(message, className) { status.textContent = message; status.className = `status ${className}`.trim(); }
function t(key) { return key.split('.').reduce((value, part) => value?.[part], copy[currentLanguage]) || key; }
function joinPath(...parts) { return parts.filter(Boolean).join('/').replace(/\/{2,}/g, '/'); }
function formatBytes(bytes) { if (!Number.isFinite(bytes) || bytes <= 0) return '0 B'; const units = ['B', 'KB', 'MB', 'GB', 'TB']; const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1); return `${(bytes / 1024 ** index).toFixed(index ? 1 : 0)} ${units[index]}`; }
function formatDate(value) { const date = new Date(value); if (Number.isNaN(date.getTime())) return '—'; return new Intl.DateTimeFormat(currentLanguage === 'vi' ? 'vi-VN' : 'en-GB', { dateStyle: 'medium', timeStyle: 'short' }).format(date); }
function localizeError(message) { const known = { 'Tên file không hợp lệ.': 'invalidFileName', 'Dung lượng file không hợp lệ.': 'invalidFileSize', 'Thư mục đích không hợp lệ.': 'invalidFolder', 'Tên hoặc đường dẫn thư mục không hợp lệ.': 'invalidFolder', 'Đường dẫn thư mục không hợp lệ.': 'invalidFolder', 'Tên thư mục đã tồn tại.': 'folderExists', 'Thư mục không tồn tại.': 'folderNotFound', 'Phiên upload không tồn tại hoặc đã kết thúc.': 'sessionNotFound', 'Chunk không hợp lệ.': 'invalidChunk', 'Kích thước chunk không hợp lệ.': 'invalidChunkSize', 'Dữ liệu chunk chưa hoàn chỉnh.': 'incompleteChunk' }; return known[message] ? t(`errors.${known[message]}`) : message; }
