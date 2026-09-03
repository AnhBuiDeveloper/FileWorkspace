const CHUNK_SIZE = 8 * 1024 * 1024;
const MAX_CONCURRENT_UPLOADS = 3;

const fileInput = document.querySelector('#file-input');
const tokenInput = document.querySelector('#upload-token');
const dropZone = document.querySelector('#drop-zone');
const panel = document.querySelector('#upload-panel');
const uploadList = document.querySelector('#upload-list');
const uploadCount = document.querySelector('#upload-count');
const status = document.querySelector('#status');

const uploads = [];
let activeTransfers = 0;

tokenInput.value = sessionStorage.getItem('upload-token') || '';
tokenInput.addEventListener('input', () => sessionStorage.setItem('upload-token', tokenInput.value));

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
    setStatus('Nhập upload token trước.', 'error');
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
        this.error = error.message || 'Không thể tạo phiên upload.';
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
        this.error = error.message || 'Upload thất bại.';
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
            reject(new Error('Phản hồi server không hợp lệ.'));
          }
          return;
        }
        reject(new Error(readXhrError(xhr) || `Upload lỗi (${xhr.status}).`));
      };
      xhr.onerror = () => reject(new Error('Không kết nối được server.'));
      xhr.onabort = () => reject(new DOMException('Upload bị tạm dừng.', 'AbortError'));
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
    const statusText = {
      preparing: 'Đang chuẩn bị…',
      queued: 'Đang chờ…',
      uploading: 'Đang upload…',
      paused: 'Đã tạm dừng',
      stopped: 'Đã dừng',
      completed: 'Hoàn tất',
      error: 'Có lỗi'
    }[this.state];

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
      controls.append(button('Pause', 'secondary', () => this.pause()));
      controls.append(button('Stop', 'danger', () => this.stop()));
    } else if (this.state === 'paused') {
      controls.append(button('Resume', 'primary', () => this.resume()));
      controls.append(button('Stop', 'danger', () => this.stop()));
    } else if (this.state === 'error') {
      controls.append(button('Stop', 'danger', () => this.stop()));
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
  uploadCount.textContent = active ? `${active} file` : `${uploads.length} file`;
}

function setStatus(message, className) {
  status.textContent = message;
  status.className = `status ${className}`.trim();
}

function formatBytes(bytes) {
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  return `${(bytes / 1024 ** index).toFixed(index ? 1 : 0)} ${units[index]}`;
}
