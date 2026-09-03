# File Upload

[Tiếng Việt](#tiếng-việt) · [English](#english)

---

## Tiếng Việt

Server upload file tự host, viết bằng ASP.NET Core. File được stream trực tiếp xuống ổ đĩa để hỗ trợ file lớn mà không cần giữ toàn bộ file trong RAM.

### Tính năng

- Chọn hoặc kéo-thả mọi loại file.
- Progress bar, phần trăm, dung lượng đã gửi và tốc độ upload.
- Xác thực upload bằng token (`UPLOAD_ACCESS_TOKEN`).
- Stream dữ liệu trực tiếp vào thư mục `Upload/`.
- Ghi file tạm với đuôi `.uploading`, sau đó đổi tên nguyên tử khi upload thành công.
- Không ghi đè file trùng tên.
- Cấu hình sẵn để chạy Windows và Ubuntu ARM64 với systemd + Nginx.

### Chạy local trên Windows

1. Tạo file `.env` tại thư mục gốc:

   ```env
   UPLOAD_ACCESS_TOKEN=thay-bang-token-dai-va-ngau-nhien
   ```

2. Chạy:

   ```powershell
   ./Start-Server.ps1
   ```

3. Mở `http://127.0.0.1:5088`, nhập token, rồi chọn file.

`.env` chứa secret và đã được Git bỏ qua. Không commit file này.

### Triển khai Ubuntu ARM64

Thư mục `deploy/` chứa:

- `file-upload.service`: systemd service, chạy ứng dụng tại `127.0.0.1:5088`.
- `file-upload.nginx.conf`: Nginx reverse proxy cổng 80, tắt giới hạn dung lượng và buffering cho upload.
- `.env.example`: mẫu biến môi trường cho server.

Thư mục triển khai mặc định là `/opt/file-upload`; file nhận được nằm trong `/opt/file-upload/Upload`.

### Bảo mật

- Dùng token dài, ngẫu nhiên và chỉ chia sẻ cho người tin cậy.
- Không public `.env`, token hoặc file upload.
- Dùng HTTPS trước khi mở dịch vụ ra Internet.
- Chỉ mở firewall/security rule cần thiết; ưu tiên VPN/Tailscale nếu dùng nội bộ.
- File upload không được serve trực tiếp bởi ứng dụng.

### License

Đây là dự án **source-available**, không phải Open Source theo định nghĩa OSI.

Mã nguồn phát hành theo [PolyForm Noncommercial License 1.0.0](LICENSE): được dùng, sửa và chia sẻ cho mục đích phi thương mại. Dùng cho mục đích thương mại cần license bằng văn bản riêng. Xem [Commercial licensing](COMMERCIAL-LICENSE.md), [NOTICE](NOTICE) và [ENFORCEMENT.md](ENFORCEMENT.md).

Đóng góp được chào đón. Xem [CONTRIBUTING.md](CONTRIBUTING.md). Lỗ hổng bảo mật: xem [SECURITY.md](SECURITY.md).

---

## English

A self-hosted file upload server built with ASP.NET Core. Files stream directly to disk, so large uploads do not need to be held in memory.

### Features

- Select or drag and drop any file type.
- Upload progress, percentage, transferred size, and speed.
- Token-protected uploads through `UPLOAD_ACCESS_TOKEN`.
- Direct streaming to the `Upload/` directory.
- Temporary `.uploading` files are atomically renamed only after a successful upload.
- Duplicate filenames are not overwritten.
- Ready for Windows, or Ubuntu ARM64 with systemd and Nginx.

### Run locally on Windows

1. Create a `.env` file in the repository root:

   ```env
   UPLOAD_ACCESS_TOKEN=replace-with-a-long-random-token
   ```

2. Run:

   ```powershell
   ./Start-Server.ps1
   ```

3. Visit `http://127.0.0.1:5088`, enter the token, then select a file.

The `.env` file contains a secret and is ignored by Git. Never commit it.

### Deploy on Ubuntu ARM64

The `deploy/` directory contains:

- `file-upload.service`: a systemd service that runs the app on `127.0.0.1:5088`.
- `file-upload.nginx.conf`: an Nginx reverse proxy on port 80 with no upload-size limit or request buffering.
- `.env.example`: server environment-variable template.

The default deployment directory is `/opt/file-upload`; uploaded files are stored in `/opt/file-upload/Upload`.

### Security

- Use a long, random token and share it only with trusted people.
- Never expose `.env`, tokens, or uploaded files.
- Use HTTPS before exposing the service to the Internet.
- Open only required firewall/security rules; prefer VPN/Tailscale for internal use.
- Uploaded files are not served directly by the application.

### License

This is **source-available** software, not Open Source under the OSI definition.

The source code is distributed under the [PolyForm Noncommercial License 1.0.0](LICENSE). You may use, modify, and distribute it for non-commercial purposes. Commercial use requires a separate written license. See [Commercial licensing](COMMERCIAL-LICENSE.md), [NOTICE](NOTICE), and [ENFORCEMENT.md](ENFORCEMENT.md).

Contributions are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md). For security issues, see [SECURITY.md](SECURITY.md).
