const CHUNK_SIZE = 8 * 1024 * 1024;
const MAX_CONCURRENT_UPLOADS = 3;
const SPEED_SAMPLE_WINDOW_MS = 4000;
const SPEED_MIN_SAMPLE_MS = 250;

const copy = {
  vi: {
    documentTitle: 'File Workspace', languageLabel: 'Ngôn ngữ', title: 'Không gian file riêng', hint: 'Nhập upload token trong thanh bên để truy cập file của bạn.', searchPlaceholder: 'Tìm trong folder hiện tại', myFiles: 'File của tôi', folders: 'Folders', privateWorkspace: 'Không gian file riêng tư', nameColumn: 'Tên', modifiedColumn: 'Chỉnh sửa', sizeColumn: 'Dung lượng', newLabel: 'Mới', openNavigation: 'Mở điều hướng', toggleFolder: 'Mở hoặc đóng folder',
    tokenLabel: 'Upload token', tokenPlaceholder: 'Nhập upload token', logout: 'Đăng xuất', loggedOut: 'Đã đăng xuất và dừng các upload đang hoạt động.', tokenRequired: 'Nhập upload token trước.',
    fileManagerKicker: 'FILE MANAGER', filesTitle: 'File của bạn', newFolder: 'Tạo folder', uploadFiles: 'Upload file', uploadFolder: 'Upload folder', refreshFiles: 'Làm mới',
    dropZoneText: 'Kéo thả file vào đây để upload vào folder hiện tại', dropZoneLabel: 'Chọn file để upload vào folder hiện tại', home: 'Upload', filesLoading: 'Đang tải nội dung…', filesEmpty: 'Folder này đang trống.', refreshFilesHint: 'Nhấn Làm mới để xem file với token hiện tại.',
    folder: 'Folder', download: 'Tải xuống', downloadSelected: 'Tải ZIP ({count})', deleteSelected: 'Xóa ({count})', deleteSelectedTitle: 'Xóa {count} mục đã chọn', selectAll: 'Chọn tất cả mục đang hiển thị', selectItem: 'Chọn {name}', downloadStarted: 'Đã bắt đầu tải file.', archiveStarted: 'Đã bắt đầu tạo ZIP.', delete: 'Xóa', deleteFile: 'Xóa file', deletePermanently: 'Xóa vĩnh viễn', deleteConfirmation: 'Xóa vĩnh viễn “{name}”? Thao tác này không thể hoàn tác.', deleteSelectionConfirmation: 'Xóa vĩnh viễn {count} mục đã chọn? Folder được chọn và toàn bộ nội dung trong đó cũng sẽ bị xóa. Thao tác này không thể hoàn tác.', fileDeleted: 'Đã xóa file.', itemsDeleted: 'Đã xóa {count} mục.', close: 'Đóng', uploadListTitle: 'Tiến trình upload',
    folderName: 'Tên folder', cancel: 'Hủy', createFolder: 'Tạo folder', creatingFolder: 'Đang tạo…', folderCreated: 'Đã tạo folder.',
    states: { preparing: 'Đang chuẩn bị…', queued: 'Đang chờ…', uploading: 'Đang upload…', paused: 'Đã tạm dừng', stopped: 'Đã dừng', completed: 'Hoàn tất', error: 'Có lỗi' },
    buttons: { pause: 'Pause', resume: 'Resume', stop: 'Stop' },
    errors: { createSession: 'Không thể tạo phiên upload.', invalidResponse: 'Phản hồi server không hợp lệ.', connection: 'Không kết nối được server.', uploadFailed: 'Upload thất bại.', unauthorized: 'Upload token không hợp lệ hoặc đã hết quyền truy cập.', invalidFileName: 'Tên file không hợp lệ.', invalidFileSize: 'Dung lượng file không hợp lệ.', invalidFolder: 'Tên hoặc đường dẫn thư mục không hợp lệ.', folderExists: 'Tên thư mục đã tồn tại.', folderNotFound: 'Thư mục không tồn tại.', fileNotFound: 'File không tồn tại.', archiveEmpty: 'Cần chọn ít nhất một file hoặc folder.', archiveNotFound: 'File hoặc folder không tồn tại.', folderHasIncompleteUpload: 'Không thể xóa folder đang có upload chưa hoàn tất.', sessionNotFound: 'Phiên upload không tồn tại hoặc đã kết thúc.', invalidChunk: 'Chunk không hợp lệ.', invalidChunkSize: 'Kích thước chunk không hợp lệ.', incompleteChunk: 'Dữ liệu chunk chưa hoàn chỉnh.', downloadFailed: 'Không thể tải file.' }
  },
  en: {
    documentTitle: 'File Workspace', languageLabel: 'Language', title: 'Private file workspace', hint: 'Enter the upload token in the sidebar to access your files.', searchPlaceholder: 'Search current folder', myFiles: 'My files', folders: 'Folders', privateWorkspace: 'Private file workspace', nameColumn: 'Name', modifiedColumn: 'Modified', sizeColumn: 'Size', newLabel: 'New', openNavigation: 'Open navigation', toggleFolder: 'Toggle folder',
    tokenLabel: 'Upload token', tokenPlaceholder: 'Enter upload token', logout: 'Log out', loggedOut: 'You have been logged out and active uploads have been stopped.', tokenRequired: 'Enter the upload token first.',
    fileManagerKicker: 'FILE MANAGER', filesTitle: 'Your files', newFolder: 'New folder', uploadFiles: 'Upload files', uploadFolder: 'Upload folder', refreshFiles: 'Refresh',
    dropZoneText: 'Drop files here to upload them to the current folder', dropZoneLabel: 'Choose files to upload to the current folder', home: 'Upload', filesLoading: 'Loading contents…', filesEmpty: 'This folder is empty.', refreshFilesHint: 'Select Refresh to view files with the current token.',
    folder: 'Folder', download: 'Download', downloadSelected: 'Download ZIP ({count})', deleteSelected: 'Delete ({count})', deleteSelectedTitle: 'Delete {count} selected items', selectAll: 'Select all visible items', selectItem: 'Select {name}', downloadStarted: 'The download has started.', archiveStarted: 'ZIP download has started.', delete: 'Delete', deleteFile: 'Delete file', deletePermanently: 'Delete permanently', deleteConfirmation: 'Permanently delete “{name}”? This cannot be undone.', deleteSelectionConfirmation: 'Permanently delete {count} selected items? Selected folders and all their contents will also be deleted. This cannot be undone.', fileDeleted: 'File deleted.', itemsDeleted: 'Deleted {count} items.', close: 'Close', uploadListTitle: 'Upload activity',
    folderName: 'Folder name', cancel: 'Cancel', createFolder: 'Create folder', creatingFolder: 'Creating…', folderCreated: 'Folder created.',
    states: { preparing: 'Preparing…', queued: 'Queued', uploading: 'Uploading…', paused: 'Paused', stopped: 'Stopped', completed: 'Completed', error: 'Error' },
    buttons: { pause: 'Pause', resume: 'Resume', stop: 'Stop' },
    errors: { createSession: 'Could not create the upload session.', invalidResponse: 'The server returned an invalid response.', connection: 'Could not connect to the server.', uploadFailed: 'Upload failed.', unauthorized: 'The upload token is invalid or no longer has access.', invalidFileName: 'The file name is invalid.', invalidFileSize: 'The file size is invalid.', invalidFolder: 'The folder name or path is invalid.', folderExists: 'A folder with this name already exists.', folderNotFound: 'The folder does not exist.', fileNotFound: 'The file does not exist.', archiveEmpty: 'Select at least one file or folder.', archiveNotFound: 'The file or folder does not exist.', folderHasIncompleteUpload: 'A folder with an incomplete upload cannot be deleted.', sessionNotFound: 'The upload session does not exist or has ended.', invalidChunk: 'The upload chunk is invalid.', invalidChunkSize: 'The upload chunk size is invalid.', incompleteChunk: 'The upload chunk is incomplete.', downloadFailed: 'Could not download the file.' }
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
const deleteFileDialog = document.querySelector('#delete-file-dialog');
const deleteFileForm = document.querySelector('#delete-file-form');
const deleteFileDialogTitle = document.querySelector('#delete-file-dialog-title');
const deleteFileDialogMessage = document.querySelector('#delete-file-dialog-message');
const closeDeleteFileDialogButton = document.querySelector('#close-delete-file-dialog');
const cancelDeleteFileButton = document.querySelector('#cancel-delete-file');
const confirmDeleteFileButton = document.querySelector('#confirm-delete-file');
const authPanel = document.querySelector('#auth-panel');
const folderTree = document.querySelector('#folder-tree');
const myFilesButton = document.querySelector('#my-files');
const workspaceSearch = document.querySelector('#workspace-search');
const downloadSelectionButton = document.querySelector('#download-selection');
const deleteSelectionButton = document.querySelector('#delete-selection');
const selectAllCheckbox = document.querySelector('#select-all');
const sidebar = document.querySelector('#sidebar');
const sidebarToggle = document.querySelector('#sidebar-toggle');
const sidebarBackdrop = document.querySelector('#sidebar-backdrop');

const uploads = [];
let activeTransfers = 0;
let currentPath = '';
let listedToken = '';
let entries = [];
let fileRequestVersion = 0;
const treeCache = new Map();
const expandedTreePaths = new Set(['']);
const newFilePaths = new Set();
const selectedPaths = new Set();
let pendingDelete;
const storedLanguage = localStorage.getItem('file-workspace-language') ?? localStorage.getItem('file-upload-language');
if (storedLanguage) {
  localStorage.setItem('file-workspace-language', storedLanguage);
  localStorage.removeItem('file-upload-language');
}
let currentLanguage = storedLanguage || (navigator.language.startsWith('vi') ? 'vi' : 'en');

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
  localStorage.setItem('file-workspace-language', currentLanguage);
  applyLanguage();
});
logoutButton.addEventListener('click', logout);
refreshFilesButton.addEventListener('click', () => loadFiles());
document.querySelectorAll('[data-upload-trigger], #upload-files').forEach(element => element.addEventListener('click', () => fileInput.click()));
uploadFolderButton.addEventListener('click', () => folderInput.click());
newFolderButton.addEventListener('click', openFolderDialog);
downloadSelectionButton.addEventListener('click', downloadSelection);
deleteSelectionButton.addEventListener('click', openDeleteSelectionDialog);
selectAllCheckbox.addEventListener('change', () => {
  const visibleEntries = getDisplayedEntries();
  visibleEntries.forEach(entry => {
    const path = joinPath(currentPath, entry.name);
    if (selectAllCheckbox.checked) selectedPaths.add(path); else selectedPaths.delete(path);
  });
  renderFileManager();
});
myFilesButton.addEventListener('click', () => loadFiles(''));
workspaceSearch.addEventListener('input', renderFileManager);
sidebarToggle.addEventListener('click', () => setSidebarOpen(!sidebar.classList.contains('is-open')));
sidebarBackdrop.addEventListener('click', () => setSidebarOpen(false));
closeFolderDialogButton.addEventListener('click', () => folderDialog.close());
cancelFolderDialogButton.addEventListener('click', () => folderDialog.close());
folderForm.addEventListener('submit', createFolder);
closeDeleteFileDialogButton.addEventListener('click', () => deleteFileDialog.close());
cancelDeleteFileButton.addEventListener('click', () => deleteFileDialog.close());
deleteFileDialog.addEventListener('close', () => { pendingDelete = undefined; confirmDeleteFileButton.disabled = false; });
deleteFileForm.addEventListener('submit', confirmDeleteFile);
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
    this.speedSamples = [];
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
      if (data.completed) this.complete();
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
          this.complete();
        }
        this.render();
      }
    } catch (error) {
      if (!['paused', 'stopped'].includes(this.state)) { this.state = 'error'; this.error = localizeError(error.message || t('errors.uploadFailed')); this.resetSpeed(); }
    } finally {
      this.xhr = null; activeTransfers -= 1; this.render(); scheduleUploads();
    }
  }

  sendChunk(chunkIndex) {
    const start = chunkIndex * CHUNK_SIZE;
    const chunk = this.file.slice(start, Math.min(start + CHUNK_SIZE, this.file.size));
    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest(); this.xhr = xhr;
      xhr.open('PUT', `/api/uploads/${this.uploadId}/chunks/${chunkIndex}`);
      xhr.setRequestHeader('Content-Type', 'application/octet-stream'); xhr.setRequestHeader('X-Upload-Token', this.token);
      xhr.upload.onprogress = event => {
        if (!event.lengthComputable || this.state !== 'uploading') return;
        const currentBytes = this.uploadedBytes + event.loaded;
        this.updateSpeed(currentBytes); this.render(currentBytes);
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

  updateSpeed(currentBytes) {
    const now = performance.now();
    this.speedSamples.push({ bytes: currentBytes, time: now });
    const cutoff = now - SPEED_SAMPLE_WINDOW_MS;
    while (this.speedSamples.length > 1 && this.speedSamples[0].time < cutoff) this.speedSamples.shift();
    const firstSample = this.speedSamples[0];
    const elapsedMs = now - firstSample.time;
    if (elapsedMs >= SPEED_MIN_SAMPLE_MS)
      this.speed = Math.max(0, (currentBytes - firstSample.bytes) / (elapsedMs / 1000));
  }

  resetSpeed() { this.speed = 0; this.speedSamples = []; }

  pause() { if (!['preparing', 'queued', 'uploading'].includes(this.state)) return; this.state = 'paused'; this.resetSpeed(); this.xhr?.abort(); this.render(); updateUploadCount(); }
  resume() { if (this.state !== 'paused') return; this.state = this.uploadId ? 'queued' : 'preparing'; this.error = ''; this.render(); scheduleUploads(); }
  async stop() { if (['stopped', 'completed'].includes(this.state)) return; this.state = 'stopped'; this.resetSpeed(); this.xhr?.abort(); this.render(); updateUploadCount(); await this.deleteSession(); }
  async deleteSession() { if (!this.uploadId) return; try { await fetch(`/api/uploads/${this.uploadId}`, { method: 'DELETE', headers: { 'X-Upload-Token': this.token } }); } catch { /* local state is already stopped */ } }

  complete() {
    if (this.state === 'completed') return;
    this.state = 'completed'; this.uploadedBytes = this.file.size; this.resetSpeed();
    newFilePaths.add(joinPath(this.targetPath, this.file.name));
    if (currentPath === this.targetPath) loadFiles();
    window.setTimeout(() => {
      const index = uploads.indexOf(this);
      if (index >= 0) uploads.splice(index, 1);
      this.element?.remove();
      updateUploadCount();
    }, 0);
  }

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
    currentPath = data.path || ''; entries = Array.isArray(data.entries) ? data.entries : []; listedToken = token;
    treeCache.set(currentPath, entries.filter(entry => entry.type === 'folder'));
    expandPathAncestors(currentPath);
    renderFileManager(); renderFolderTree(); closeSidebarOnNavigation();
  } catch (error) {
    if (requestVersion !== fileRequestVersion) return;
    entries = []; filesList.replaceChildren(); filesStatus.textContent = localizeError(error.message || t('errors.connection'));
  } finally { if (requestVersion === fileRequestVersion) refreshFilesButton.disabled = false; }
}

function renderFileManager() {
  renderBreadcrumbs(); filesList.replaceChildren();
  const displayedEntries = getDisplayedEntries();
  if (!displayedEntries.length) { filesStatus.textContent = t('filesEmpty'); return; }
  filesStatus.textContent = `${displayedEntries.length} ${currentLanguage === 'vi' ? 'mục' : displayedEntries.length === 1 ? 'item' : 'items'}`;
  displayedEntries.forEach(entry => {
    const row = document.createElement('article'); row.className = `file-entry ${entry.type}`; row.setAttribute('role', 'listitem');
    const path = joinPath(currentPath, entry.name);
    const selection = document.createElement('label'); selection.className = 'entry-select';
    const checkbox = document.createElement('input'); checkbox.type = 'checkbox'; checkbox.checked = selectedPaths.has(path); checkbox.setAttribute('aria-label', t('selectItem').replace('{name}', entry.name));
    checkbox.addEventListener('change', () => { if (checkbox.checked) selectedPaths.add(path); else selectedPaths.delete(path); updateSelectionControls(displayedEntries); });
    selection.append(checkbox);
    const info = document.createElement('div'); info.className = 'entry-info';
    const icon = document.createElement('span'); icon.className = 'entry-icon'; icon.textContent = entry.type === 'folder' ? '▰' : '▱';
    const name = document.createElement('button'); name.className = 'entry-name'; name.type = 'button'; name.textContent = entry.name; name.title = entry.name;
    if (entry.type === 'folder') name.addEventListener('click', () => loadFiles(joinPath(currentPath, entry.name)));
    else name.addEventListener('click', () => downloadFile(entry));
    const nameWrap = document.createElement('div'); nameWrap.className = 'entry-name-wrap'; nameWrap.append(name);
    if (newFilePaths.has(joinPath(currentPath, entry.name))) {
      const badge = document.createElement('span'); badge.className = 'new-badge'; badge.textContent = t('newLabel'); nameWrap.append(badge);
    }
    info.append(icon, nameWrap);
    const modified = document.createElement('span'); modified.className = 'entry-meta'; modified.textContent = formatDate(entry.modifiedAtUtc);
    const size = document.createElement('span'); size.className = 'entry-meta'; size.textContent = entry.type === 'folder' ? t('folder') : formatBytes(entry.bytes);
    const actions = document.createElement('div'); actions.className = 'entry-actions';
    if (entry.type === 'folder') actions.append(button('›', 'open-folder', () => loadFiles(joinPath(currentPath, entry.name)), entry.name));
    else actions.append(button(t('download'), 'download', () => downloadFile(entry)), button(t('delete'), 'danger', () => openDeleteFileDialog(entry), t('deleteFile')));
    row.append(selection, info, modified, size, actions); filesList.append(row);
  });
  updateSelectionControls(displayedEntries);
}

function getDisplayedEntries() {
  const query = workspaceSearch.value.trim().toLocaleLowerCase();
  return query ? entries.filter(entry => entry.name.toLocaleLowerCase().includes(query)) : entries;
}

function updateSelectionControls(visibleEntries = getDisplayedEntries()) {
  const selectedVisible = visibleEntries.filter(entry => selectedPaths.has(joinPath(currentPath, entry.name))).length;
  selectAllCheckbox.checked = visibleEntries.length > 0 && selectedVisible === visibleEntries.length;
  selectAllCheckbox.indeterminate = selectedVisible > 0 && selectedVisible < visibleEntries.length;
  downloadSelectionButton.disabled = selectedPaths.size === 0;
  downloadSelectionButton.textContent = t('downloadSelected').replace('{count}', String(selectedPaths.size));
  deleteSelectionButton.disabled = selectedPaths.size === 0;
  deleteSelectionButton.textContent = t('deleteSelected').replace('{count}', String(selectedPaths.size));
  downloadSelectionButton.hidden = selectedPaths.size === 0;
  deleteSelectionButton.hidden = selectedPaths.size === 0;
}

function renderFolderTree() {
  folderTree.replaceChildren();
  if (!tokenInput.value.trim()) return;
  folderTree.append(createTreeBranch('', t('home'), true));
}

function createTreeBranch(path, name, isRoot = false) {
  const branch = document.createElement('div'); branch.className = 'tree-branch';
  const row = document.createElement('div'); row.className = 'tree-row';
  const children = treeCache.get(path);
  const expanded = expandedTreePaths.has(path);
  const expander = document.createElement('button'); expander.type = 'button'; expander.className = 'tree-expander';
  expander.textContent = expanded ? '⌄' : '›'; expander.setAttribute('aria-label', `${t('toggleFolder')}: ${name}`);
  expander.addEventListener('click', () => toggleTreePath(path));
  const item = document.createElement('button'); item.type = 'button'; item.className = `tree-item${path === currentPath ? ' active' : ''}`; item.setAttribute('aria-label', `${t('folder')}: ${name}`);
  const icon = document.createElement('span'); icon.className = 'tree-icon'; icon.textContent = isRoot ? '⌂' : '▰';
  const label = document.createElement('span'); label.textContent = name; label.title = name; item.append(icon, label);
  item.addEventListener('click', () => loadFiles(path));
  row.append(expander, item); branch.append(row);
  if (expanded) {
    const childContainer = document.createElement('div'); childContainer.className = 'tree-children';
    if (children) children.forEach(entry => childContainer.append(createTreeBranch(joinPath(path, entry.name), entry.name)));
    branch.append(childContainer);
  }
  return branch;
}

async function toggleTreePath(path) {
  if (expandedTreePaths.has(path)) { expandedTreePaths.delete(path); renderFolderTree(); return; }
  expandedTreePaths.add(path);
  if (!treeCache.has(path)) {
    try {
      const response = await fetch(`/api/files?path=${encodeURIComponent(path)}`, { headers: { 'X-Upload-Token': tokenInput.value.trim() } });
      const data = await readResponse(response);
      if (!response.ok) throw new Error(data.error || t('errors.connection'));
      treeCache.set(path, (data.entries || []).filter(entry => entry.type === 'folder'));
    } catch { expandedTreePaths.delete(path); }
  }
  renderFolderTree();
}

function expandPathAncestors(path) {
  expandedTreePaths.add('');
  const parts = path ? path.split('/') : [];
  parts.forEach((_, index) => expandedTreePaths.add(parts.slice(0, index + 1).join('/')));
}

function setSidebarOpen(isOpen) {
  sidebar.classList.toggle('is-open', isOpen); sidebarToggle.setAttribute('aria-expanded', String(isOpen)); sidebarBackdrop.hidden = !isOpen;
}

function closeSidebarOnNavigation() {
  if (window.matchMedia('(max-width: 900px)').matches) setSidebarOpen(false);
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
    folderDialog.close(); treeCache.delete(currentPath); setStatus(t('folderCreated'), 'success'); loadFiles();
  } catch (error) { folderDialogStatus.textContent = localizeError(error.message || t('errors.invalidFolder')); }
  finally { submit.disabled = false; }
}

function downloadFile(entry) {
  const token = tokenInput.value.trim(); if (!token) return;
  const form = document.createElement('form'); form.method = 'post'; form.action = '/api/files/download'; form.target = '_blank'; form.hidden = true;
  [['path', joinPath(currentPath, entry.name)], ['token', token]].forEach(([name, value]) => { const input = document.createElement('input'); input.type = 'hidden'; input.name = name; input.value = value; form.append(input); });
  document.body.append(form); form.submit(); form.remove(); filesStatus.textContent = t('downloadStarted');
}

function downloadSelection() {
  const token = tokenInput.value.trim();
  if (!token || selectedPaths.size === 0) return;
  const form = document.createElement('form'); form.method = 'post'; form.action = '/api/files/archive'; form.target = '_blank'; form.hidden = true;
  [['token', token], ...[...selectedPaths].map(path => ['path', path])].forEach(([name, value]) => {
    const input = document.createElement('input'); input.type = 'hidden'; input.name = name; input.value = value; form.append(input);
  });
  document.body.append(form); form.submit(); form.remove(); setStatus(t('archiveStarted'), 'success');
}

function openDeleteFileDialog(entry) {
  if (!tokenInput.value.trim()) { setStatus(t('tokenRequired'), 'error'); tokenInput.focus(); return; }
  openDeleteDialog({ paths: [joinPath(currentPath, entry.name)], name: entry.name });
}

function openDeleteSelectionDialog() {
  if (!tokenInput.value.trim() || selectedPaths.size === 0) return;
  openDeleteDialog({ paths: [...selectedPaths] });
}

function openDeleteDialog(request) {
  pendingDelete = request;
  updateDeleteDialogCopy();
  deleteFileDialog.showModal();
  cancelDeleteFileButton.focus();
}

async function confirmDeleteFile(event) {
  event.preventDefault();
  const request = pendingDelete; const token = tokenInput.value.trim();
  if (!request || !token) return;
  confirmDeleteFileButton.disabled = true;
  try {
    const response = await fetch('/api/files/delete', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Upload-Token': token }, body: JSON.stringify({ paths: request.paths }) });
    const data = await readResponse(response);
    if (!response.ok) throw new Error(response.status === 401 ? t('errors.unauthorized') : data.error || t('errors.fileNotFound'));
    removeDeletedPaths(request.paths); treeCache.clear(); deleteFileDialog.close();
    setStatus(request.paths.length === 1 ? t('fileDeleted') : t('itemsDeleted').replace('{count}', String(request.paths.length)), 'success');
    await loadFiles();
  } catch (error) { setStatus(localizeError(error.message || t('errors.fileNotFound')), 'error'); }
  finally { if (deleteFileDialog.open) confirmDeleteFileButton.disabled = false; }
}

function updateDeleteDialogCopy() {
  if (!pendingDelete) return;
  const count = String(pendingDelete.paths.length);
  const isSingleFile = Boolean(pendingDelete.name);
  deleteFileDialogTitle.textContent = isSingleFile ? t('deleteFile') : t('deleteSelectedTitle').replace('{count}', count);
  deleteFileDialogMessage.textContent = isSingleFile
    ? t('deleteConfirmation').replace('{name}', pendingDelete.name)
    : t('deleteSelectionConfirmation').replace('{count}', count);
}

function removeDeletedPaths(deletedPaths) {
  [newFilePaths, selectedPaths, expandedTreePaths].forEach(paths => {
    for (const path of paths)
      if (deletedPaths.some(deletedPath => path === deletedPath || path.startsWith(`${deletedPath}/`))) paths.delete(path);
  });
  expandedTreePaths.add('');
}

function logout() {
  uploads.filter(upload => !['completed', 'stopped', 'error'].includes(upload.state)).forEach(upload => upload.stop()); selectedPaths.clear();
  localStorage.removeItem('upload-token'); tokenInput.value = ''; entries = []; listedToken = ''; currentPath = ''; treeCache.clear(); expandedTreePaths.clear(); expandedTreePaths.add(''); updateAuthControls(); setStatus(t('loggedOut'), 'success'); tokenInput.focus();
}

function updateAuthControls() {
  const token = tokenInput.value.trim(); logoutButton.hidden = !token; filesPanel.hidden = !token; authPanel.hidden = !!token;
  if (!token) { filesList.replaceChildren(); filesStatus.textContent = ''; breadcrumbs.replaceChildren(); folderTree.replaceChildren(); return; }
  if (listedToken !== token) { entries = []; filesList.replaceChildren(); filesStatus.textContent = t('refreshFilesHint'); }
}

function updateUploadCount() {
  const active = uploads.filter(upload => ['preparing', 'queued', 'uploading', 'paused'].includes(upload.state)).length;
  uploadPanel.hidden = active === 0;
  uploadCount.textContent = currentLanguage === 'vi' ? `${active} file` : `${active} file${active === 1 ? '' : 's'}`;
}

function applyLanguage() {
  document.documentElement.lang = currentLanguage; document.title = t('documentTitle');
  document.querySelectorAll('[data-i18n]').forEach(element => { element.textContent = t(element.dataset.i18n); });
  document.querySelectorAll('[data-i18n-placeholder]').forEach(element => { element.placeholder = t(element.dataset.i18nPlaceholder); });
  document.querySelectorAll('[data-i18n-title]').forEach(element => { element.title = t(element.dataset.i18nTitle); });
  document.querySelectorAll('[data-i18n-aria-label]').forEach(element => { element.setAttribute('aria-label', t(element.dataset.i18nAriaLabel)); });
  updateDeleteDialogCopy();
  languageSelect.setAttribute('aria-label', t('languageLabel')); filesPanel.setAttribute('aria-label', t('filesTitle')); uploadPanel.setAttribute('aria-label', t('uploadListTitle'));
  uploads.forEach(upload => upload.render()); renderFileManager(); renderFolderTree(); updateUploadCount(); updateSelectionControls();
}

function button(label, className, onClick, title = '') { const element = document.createElement('button'); element.type = 'button'; element.className = `button ${className}`; element.textContent = label; if (title) element.title = title; element.addEventListener('click', onClick); return element; }
async function readResponse(response) { try { return await response.json(); } catch { return {}; } }
function readXhrError(xhr) { try { return JSON.parse(xhr.responseText).error; } catch { return ''; } }
function setStatus(message, className) { status.textContent = message; status.className = `status ${className}`.trim(); }
function t(key) { return key.split('.').reduce((value, part) => value?.[part], copy[currentLanguage]) || key; }
function joinPath(...parts) { return parts.filter(Boolean).join('/').replace(/\/{2,}/g, '/'); }
function formatBytes(bytes) { if (!Number.isFinite(bytes) || bytes <= 0) return '0 B'; const units = ['B', 'KB', 'MB', 'GB', 'TB']; const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1); return `${(bytes / 1024 ** index).toFixed(index ? 1 : 0)} ${units[index]}`; }
function formatDate(value) { const date = new Date(value); if (Number.isNaN(date.getTime())) return '—'; return new Intl.DateTimeFormat(currentLanguage === 'vi' ? 'vi-VN' : 'en-GB', { dateStyle: 'medium', timeStyle: 'short' }).format(date); }
function localizeError(message) { const known = { 'Tên file không hợp lệ.': 'invalidFileName', 'Dung lượng file không hợp lệ.': 'invalidFileSize', 'Thư mục đích không hợp lệ.': 'invalidFolder', 'Tên hoặc đường dẫn thư mục không hợp lệ.': 'invalidFolder', 'Đường dẫn thư mục không hợp lệ.': 'invalidFolder', 'Tên thư mục đã tồn tại.': 'folderExists', 'Thư mục không tồn tại.': 'folderNotFound', 'File không tồn tại.': 'fileNotFound', 'Cần chọn ít nhất một file hoặc folder.': 'archiveEmpty', 'File hoặc folder không tồn tại.': 'archiveNotFound', 'Không thể xóa folder đang có upload chưa hoàn tất.': 'folderHasIncompleteUpload', 'Phiên upload không tồn tại hoặc đã kết thúc.': 'sessionNotFound', 'Chunk không hợp lệ.': 'invalidChunk', 'Kích thước chunk không hợp lệ.': 'invalidChunkSize', 'Dữ liệu chunk chưa hoàn chỉnh.': 'incompleteChunk' }; return known[message] ? t(`errors.${known[message]}`) : message; }
