const fileInput = document.querySelector('#file-input');
const tokenInput = document.querySelector('#upload-token');
const dropZone = document.querySelector('#drop-zone');
const panel = document.querySelector('#upload-panel');
const fileName = document.querySelector('#file-name');
const percent = document.querySelector('#percent');
const barFill = document.querySelector('#bar-fill');
const transferred = document.querySelector('#transferred');
const speed = document.querySelector('#speed');
const status = document.querySelector('#status');

tokenInput.value = sessionStorage.getItem('upload-token') || '';
tokenInput.addEventListener('input', () => sessionStorage.setItem('upload-token', tokenInput.value));

fileInput.addEventListener('change', () => upload(fileInput.files[0]));
['dragenter', 'dragover'].forEach(event => dropZone.addEventListener(event, e => {
  e.preventDefault();
  dropZone.classList.add('dragging');
}));
['dragleave', 'drop'].forEach(event => dropZone.addEventListener(event, e => {
  e.preventDefault();
  dropZone.classList.remove('dragging');
}));
dropZone.addEventListener('drop', e => upload(e.dataTransfer.files[0]));

function upload(file) {
  if (!file) return;
  if (!tokenInput.value) {
    status.textContent = 'Nhập upload token trước.';
    status.className = 'status error';
    tokenInput.focus();
    return;
  }

  panel.hidden = false;
  fileName.textContent = file.name;
  status.textContent = 'Đang upload…';
  status.className = 'status';
  updateProgress(0, file.size, 0);

  const request = new XMLHttpRequest();
  let lastLoaded = 0;
  let lastTime = performance.now();

  request.open('POST', '/api/upload');
  request.setRequestHeader('Content-Type', 'application/octet-stream');
  request.setRequestHeader('X-File-Name', encodeURIComponent(file.name));
  request.setRequestHeader('X-Upload-Token', tokenInput.value);
  request.upload.onprogress = event => {
    if (!event.lengthComputable) return;
    const now = performance.now();
    const elapsed = Math.max((now - lastTime) / 1000, 0.001);
    const currentSpeed = (event.loaded - lastLoaded) / elapsed;
    updateProgress(event.loaded, event.total, currentSpeed);
    lastLoaded = event.loaded;
    lastTime = now;
  };
  request.onload = () => {
    if (request.status >= 200 && request.status < 300) {
      updateProgress(file.size, file.size, 0);
      status.textContent = 'Upload xong.';
      status.className = 'status success';
    } else {
      status.textContent = `Upload lỗi (${request.status}).`;
      status.className = 'status error';
    }
  };
  request.onerror = () => {
    status.textContent = 'Không kết nối được server.';
    status.className = 'status error';
  };
  request.send(file);
}

function updateProgress(done, total, bytesPerSecond) {
  const value = total ? Math.round((done / total) * 100) : 0;
  percent.textContent = `${value}%`;
  barFill.style.width = `${value}%`;
  transferred.textContent = `${formatBytes(done)} / ${formatBytes(total)}`;
  speed.textContent = `${formatBytes(bytesPerSecond)}/s`;
}

function formatBytes(bytes) {
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  return `${(bytes / 1024 ** index).toFixed(index ? 1 : 0)} ${units[index]}`;
}
