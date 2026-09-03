const CHUNK_SIZE = 8 * 1024 * 1024;
const MAX_CONCURRENT_UPLOADS = 3;

const fileInput = document.querySelector('#file-input');
const tokenInput = document.querySelector('#upload-token');
const dropZone = document.querySelector('#drop-zone');
const panel = document.querySelector('#upload-panel');
const uploadList = document.querySelector('#upload-list');
const uploadCount = document.querySelector('#upload-count');
const status = document.querySelector('#status');
const languageSelect = document.querySelector('#language-select');
const logoutButton = document.querySelector('#logout-button');

const translations = {
  vi: {
    documentTitle: 'File Upload',
    languageLabel: 'Ngôn ngữ',
    title: 'Gửi file nhanh',
    hint: 'Chọn nhiều file. Mỗi file có tiến độ riêng và có thể pause, resume hoặc stop.',
    tokenLabel: 'Upload token',
    tokenPlaceholder: 'Nhập token để upload',
    logout: 'Đăng xuất',
    chooseFile: 'Chọn file',
    orDrop: 'hoặc kéo thả vào đây',
    pickerNote: 'Có thể chọn nhiều file · tối đa 3 file truyền đồng thời',
    uploadListTitle: 'Đang gửi file',
    tokenRequired: 'Nhập upload token trước.',
    loggedOut: 'Đã đăng xuất và dừng các upload đang hoạt động.',
    states: { preparing: 'Đang chuẩn bị…', queued: 'Đang chờ…', uploading: 'Đang upload…', paused: 'Đã tạm dừng', stopped: 'Đã dừng', completed: 'Hoàn tất', error: 'Có lỗi' },
    buttons: { pause: 'Pause', resume: 'Resume', stop: 'Stop' },
    errors: {
      createSession: 'Không thể tạo phiên upload.',
      invalidResponse: 'Phản hồi server không hợp lệ.',
      connection: 'Không kết nối được server.',
      uploadFailed: 'Upload thất bại.',
      invalidFileName: 'Tên file không hợp lệ.',
      invalidFileSize: 'Dung lượng file không hợp lệ.',
      sessionNotFound: 'Phiên upload không tồn tại hoặc đã kết thúc.',
      invalidChunk: 'Chunk không hợp lệ.',
      invalidChunkSize: 'Kích thước chunk không hợp lệ.',
      incompleteChunk: 'Dữ liệu chunk chưa hoàn chỉnh.'
    }
  },
  en: {
    documentTitle: 'File Upload',
    languageLabel: 'Language',
    title: 'Fast file upload',
    hint: 'Select multiple files. Each upload has its own progress and can be paused, resumed, or stopped.',
    tokenLabel: 'Upload token',
    tokenPlaceholder: 'Enter upload token',
    logout: 'Log out',
    chooseFile: 'Choose files',
    orDrop: 'or drag and drop them here',
    pickerNote: 'Multiple files supported · up to 3 files upload at once',
    uploadListTitle: 'Uploads',
    tokenRequired: 'Enter the upload token first.',
    loggedOut: 'You have been logged out and active uploads have been stopped.',
    states: { preparing: 'Preparing…', queued: 'Queued', uploading: 'Uploading…', paused: 'Paused', stopped: 'Stopped', completed: 'Completed', error: 'Error' },
    buttons: { pause: 'Pause', resume: 'Resume', stop: 'Stop' },
    errors: {
      createSession: 'Could not create the upload session.',
      invalidResponse: 'The server returned an invalid response.',
      connection: 'Could not connect to the server.',
      uploadFailed: 'Upload failed.',
      invalidFileName: 'The file name is invalid.',
      invalidFileSize: 'The file size is invalid.',
      sessionNotFound: 'The upload session does not exist or has ended.',
      invalidChunk: 'The upload chunk is invalid.',
      invalidChunkSize: 'The upload chunk size is invalid.',
      incompleteChunk: 'The upload chunk is incomplete.'
    }
  }
};

const uploads = [];
let activeTransfers = 0;
let currentLanguage = localStorage.getItem('file-upload-language') || (navigator.language.startsWith('vi') ? 'vi' : 'en');

tokenInput.value = localStorage.getItem('upload-token') || '';
tokenInput.addEventListener('input', () => {
  const token = tokenInput.value.trim();
  if (token) localStorage.setItem('upload-token', token);
  else localStorage.removeItem('upload-token');
  updateAuthControls();
});
logoutButton.addEventListener('click', logout);
languageSelect.value = currentLanguage;
languageSelect.addEventListener('change', () => {
  currentLanguage = languageSelect.value;
  localStorage.setItem('file-upload-language', currentLanguage);
  applyLanguage();
});
applyLanguage();
updateAuthControls();

fileInput.addEventListener('change', () => {
  addFiles(fileInput.files);
  fileInput.value = '';
});

['dragenter', 'dragover'].forEach(event => dropZone.addEventListener(event, e => {
  e.preventDefault();
  dropZone.classList.add('dragging');
}));
['dragleave', 'drop'].forEach(event => dropZone.addEventListener(event, e => {
  e.preventDefault();
  dropZone.classList.remove('dragging');
}));
dropZone.addEventListener('drop', e => addFiles(e.dataTransfer.files));

function addFiles(fileList) {
  const files = [...fileList].filter(file => file instanceof File);
  if (!files.length) return;

  const token = tokenInput.value.trim();
  if (!token) {
    setStatus(t('tokenRequired'), 'error');
    tokenInput.focus();
    return;
  }

  setStatus('', '');
  files.forEach(file => {
    const upload = new UploadTask(file, token);
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
  constructor(file, token) {
    this.file = file;
    this.token = token;
    this.uploadId = null;
    this.state = 'preparing';
    this.uploadedBytes = 0;
    this.nextChunk = 0;
    this.xhr = null;
    this.speed = 0;
    this.error = '';
    this.element = null;
  }

  get totalChunks() {
    return Math.ceil(this.file.size / CHUNK_SIZE);
  }

  async initialize() {
    try {
      const response = await fetch('/api/uploads', {
        method: 'POST',
        headers: {
          'X-Upload-Token': this.token,
          'X-File-Name': encodeURIComponent(this.file.name),
          'X-File-Size': String(this.file.size)
        }
      });
      const data = await readResponse(response);
      if (!response.ok) throw new Error(data.error || `Upload lỗi (${response.status}).`);

      this.uploadId = data.uploadId;
      this.uploadedBytes = data.uploadedBytes || 0;
      if (this.state === 'stopped') {
        await this.deleteSession();
        return;
      }

      if (data.completed) {
        this.state = 'completed';
        this.uploadedBytes = this.file.size;
      } else {
        this.state = this.state === 'paused' ? 'paused' : 'queued';
        scheduleUploads();
      }
    } catch (error) {
      if (this.state !== 'stopped') {
        this.state = 'error';
        this.error = localizeError(error.message || t('errors.createSession'));
      }
    }
    this.render();
    updateUploadCount();
  }

  async transfer() {
    if (this.state !== 'queued') return;
    this.state = 'uploading';
    activeTransfers += 1;
    this.render();
    updateUploadCount();

    try {
      while (this.state === 'uploading' && this.nextChunk < this.totalChunks) {
        const result = await this.sendChunk(this.nextChunk);
        if (this.state !== 'uploading') break;

        this.uploadedBytes = result.uploadedBytes;
        this.nextChunk += 1;
        if (result.completed || this.nextChunk === this.totalChunks) {
          this.state = 'completed';
          this.uploadedBytes = this.file.size;
          this.speed = 0;
        }
        this.render();
      }
    } catch (error) {
      if (this.state !== 'paused' && this.state !== 'stopped') {
        this.state = 'error';
        this.error = localizeError(error.message || t('errors.uploadFailed'));
      }
    } finally {
      this.xhr = null;
      activeTransfers -= 1;
      this.render();
      scheduleUploads();
    }
  }

  sendChunk(chunkIndex) {
    const start = chunkIndex * CHUNK_SIZE;
    const end = Math.min(start + CHUNK_SIZE, this.file.size);
    const chunk = this.file.slice(start, end);

    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();
      this.xhr = xhr;
      let lastLoaded = 0;
      let lastTime = performance.now();

      xhr.open('PUT', `/api/uploads/${this.uploadId}/chunks/${chunkIndex}`);
      xhr.setRequestHeader('Content-Type', 'application/octet-stream');
      xhr.setRequestHeader('X-Upload-Token', this.token);
      xhr.upload.onprogress = event => {
        if (!event.lengthComputable || this.state !== 'uploading') return;
        const now = performance.now();
        const elapsed = Math.max((now - lastTime) / 1000, 0.001);
        this.speed = (event.loaded - lastLoaded) / elapsed;
        lastLoaded = event.loaded;
        lastTime = now;
        this.render(this.uploadedBytes + event.loaded);
      };
      xhr.onload = () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          try {
            resolve(JSON.parse(xhr.responseText));
          } catch {
            reject(new Error(t('errors.invalidResponse')));
          }
          return;
        }
        reject(new Error(readXhrError(xhr) || `Upload lỗi (${xhr.status}).`));
      };
      xhr.onerror = () => reject(new Error(t('errors.connection')));
      xhr.onabort = () => reject(new DOMException(t('states.paused'), 'AbortError'));
      xhr.send(chunk);
    });
  }

  pause() {
    if (!['preparing', 'queued', 'uploading'].includes(this.state)) return;
    this.state = 'paused';
    this.speed = 0;
    this.xhr?.abort();
    this.render();
    updateUploadCount();
  }

  resume() {
    if (this.state !== 'paused') return;
    this.state = this.uploadId ? 'queued' : 'preparing';
    this.error = '';
    this.render();
    scheduleUploads();
  }

  async stop() {
    if (['stopped', 'completed'].includes(this.state)) return;
    this.state = 'stopped';
    this.speed = 0;
    this.xhr?.abort();
    this.render();
    updateUploadCount();
    await this.deleteSession();
  }

  async deleteSession() {
    if (!this.uploadId) return;
    try {
      await fetch(`/api/uploads/${this.uploadId}`, {
        method: 'DELETE',
        headers: { 'X-Upload-Token': this.token }
      });
    } catch {
      // The upload has already stopped locally. A stale server session is harmless and expires on restart.
    }
  }

  render(displayedBytes = this.uploadedBytes) {
    if (!this.element) {
      this.element = document.createElement('article');
      this.element.className = 'upload-item';
      uploadList.append(this.element);
    }

    const percent = this.file.size ? Math.min(100, Math.round((displayedBytes / this.file.size) * 100)) : 100;
    const statusText = t(`states.${this.state}`);

    this.element.className = `upload-item ${this.state}`;
    this.element.replaceChildren();

    const header = document.createElement('div');
    header.className = 'file-line';
    const name = document.createElement('strong');
    name.textContent = this.file.name;
    name.title = this.file.name;
    const state = document.createElement('span');
    state.className = 'upload-state';
    state.textContent = this.state === 'error' ? this.error : statusText;
    header.append(name, state);

    const progress = document.createElement('div');
    progress.className = 'bar';
    progress.setAttribute('role', 'progressbar');
    progress.setAttribute('aria-valuenow', String(percent));
    progress.setAttribute('aria-valuemin', '0');
    progress.setAttribute('aria-valuemax', '100');
    const fill = document.createElement('div');
    fill.style.width = `${percent}%`;
    progress.append(fill);

    const footer = document.createElement('div');
    footer.className = 'upload-footer';
    const stats = document.createElement('span');
    stats.className = 'stats';
    stats.textContent = `${formatBytes(displayedBytes)} / ${formatBytes(this.file.size)} · ${this.state === 'uploading' ? `${formatBytes(this.speed)}/s` : `${percent}%`}`;
    const controls = document.createElement('div');
    controls.className = 'upload-controls';

    if (['preparing', 'queued', 'uploading'].includes(this.state)) {
      controls.append(button(t('buttons.pause'), 'secondary', () => this.pause()));
      controls.append(button(t('buttons.stop'), 'danger', () => this.stop()));
    } else if (this.state === 'paused') {
      controls.append(button(t('buttons.resume'), 'primary', () => this.resume()));
      controls.append(button(t('buttons.stop'), 'danger', () => this.stop()));
    } else if (this.state === 'error') {
      controls.append(button(t('buttons.stop'), 'danger', () => this.stop()));
    }

    footer.append(stats, controls);
    this.element.append(header, progress, footer);
  }
}

function button(label, className, onClick) {
  const element = document.createElement('button');
  element.type = 'button';
  element.className = `button ${className}`;
  element.textContent = label;
  element.addEventListener('click', onClick);
  return element;
}

async function readResponse(response) {
  try {
    return await response.json();
  } catch {
    return {};
  }
}

function readXhrError(xhr) {
  try {
    return JSON.parse(xhr.responseText).error;
  } catch {
    return '';
  }
}

function updateUploadCount() {
  const active = uploads.filter(upload => ['preparing', 'queued', 'uploading', 'paused'].includes(upload.state)).length;
  panel.hidden = uploads.length === 0;
  const count = active || uploads.length;
  uploadCount.textContent = currentLanguage === 'vi' ? `${count} file` : `${count} file${count === 1 ? '' : 's'}`;
}

function setStatus(message, className) {
  status.textContent = message;
  status.className = `status ${className}`.trim();
}

function logout() {
  uploads
    .filter(upload => !['completed', 'stopped', 'error'].includes(upload.state))
    .forEach(upload => upload.stop());
  localStorage.removeItem('upload-token');
  tokenInput.value = '';
  updateAuthControls();
  setStatus(t('loggedOut'), 'success');
  tokenInput.focus();
}

function updateAuthControls() {
  logoutButton.hidden = !tokenInput.value.trim();
}

function applyLanguage() {
  document.documentElement.lang = currentLanguage;
  document.title = t('documentTitle');
  document.querySelectorAll('[data-i18n]').forEach(element => {
    element.textContent = t(element.dataset.i18n);
  });
  document.querySelectorAll('[data-i18n-placeholder]').forEach(element => {
    element.placeholder = t(element.dataset.i18nPlaceholder);
  });
  languageSelect.setAttribute('aria-label', t('languageLabel'));
  panel.setAttribute('aria-label', t('uploadListTitle'));
  uploads.forEach(upload => upload.render());
  updateUploadCount();
}

function t(key) {
  return key.split('.').reduce((value, part) => value?.[part], translations[currentLanguage]) || key;
}

function localizeError(message) {
  const knownErrors = {
    'Tên file không hợp lệ.': 'invalidFileName',
    'Dung lượng file không hợp lệ.': 'invalidFileSize',
    'Phiên upload không tồn tại hoặc đã kết thúc.': 'sessionNotFound',
    'Chunk không hợp lệ.': 'invalidChunk',
    'Kích thước chunk không hợp lệ.': 'invalidChunkSize',
    'Dữ liệu chunk chưa hoàn chỉnh.': 'incompleteChunk'
  };
  return knownErrors[message] ? t(`errors.${knownErrors[message]}`) : message;
}

function formatBytes(bytes) {
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  return `${(bytes / 1024 ** index).toFixed(index ? 1 : 0)} ${units[index]}`;
}
