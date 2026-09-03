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
- Chạy được trên bất kỳ máy chủ nào hỗ trợ .NET/ASP.NET Core 10; có mẫu Windows và Linux systemd + Nginx.

### Yêu cầu

- .NET SDK 10 để build/chạy từ source, hoặc ASP.NET Core Runtime 10 để chạy bản publish.
- Quyền ghi vào thư mục `Upload/` tại content root của ứng dụng.
- `UPLOAD_ACCESS_TOKEN` bắt buộc; ứng dụng sẽ không khởi động nếu thiếu token.

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

### Chạy trên server bất kỳ

Bạn chọn Windows, Linux x64/ARM64, container, VM, bare metal hoặc cloud provider tùy nhu cầu. Cách tối thiểu sau khi đã publish đúng runtime identifier là:

```text
dotnet FileUpload.dll --urls http://127.0.0.1:5088
```

Đặt `UPLOAD_ACCESS_TOKEN` qua biến môi trường hoặc file `.env` cạnh `FileUpload.dll`. Với production, ưu tiên secret manager/biến môi trường của nền tảng thay vì copy token vào source repository.

### Mẫu Linux systemd + Nginx

Thư mục `deploy/` là **mẫu triển khai Linux**, không phải yêu cầu bắt buộc:

- `file-upload.service`: systemd service, chạy ứng dụng tại `127.0.0.1:5088`.
- `file-upload.nginx.conf`: Nginx reverse proxy cổng 80, tắt giới hạn dung lượng và buffering cho upload.
- `.env.example`: mẫu biến môi trường cho server.

Các mẫu này dùng `/opt/file-upload`, user/group `fileupload`, và cổng nội bộ `5088`. Hãy đổi path, user, domain, port và TLS theo server của bạn. Xem [deploy/README.md](deploy/README.md).

### Bảo mật

- Dùng token dài, ngẫu nhiên và chỉ chia sẻ cho người tin cậy.
- Không public `.env`, token hoặc file upload.
- Dùng HTTPS trước khi mở dịch vụ ra Internet; Nginx sample hiện chỉ là HTTP và không tự cấp TLS certificate.
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
- Runs on any host that supports .NET/ASP.NET Core 10; Windows and Linux systemd + Nginx examples are included.

### Requirements

- .NET SDK 10 to build/run from source, or ASP.NET Core Runtime 10 to run published output.
- Write access to the `Upload/` directory in the application's content root.
- `UPLOAD_ACCESS_TOKEN` is required; the application will not start without it.

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

### Run on any server

Choose Windows, Linux x64/ARM64, containers, VMs, bare metal, or any cloud provider that suits you. After publishing for the right runtime identifier, the minimum command is:

```text
dotnet FileUpload.dll --urls http://127.0.0.1:5088
```

Set `UPLOAD_ACCESS_TOKEN` through an environment variable or a `.env` file next to `FileUpload.dll`. For production, prefer your platform's secret manager or environment-variable facility rather than placing a token in source control.

### Linux systemd + Nginx example

The `deploy/` directory is a **Linux deployment example**, not a hosting requirement:

- `file-upload.service`: a systemd service that runs the app on `127.0.0.1:5088`.
- `file-upload.nginx.conf`: an Nginx reverse proxy on port 80 with no upload-size limit or request buffering.
- `.env.example`: server environment-variable template.

These templates use `/opt/file-upload`, the `fileupload` user/group, and internal port `5088`. Change paths, user, domain, port, and TLS for your own server. See [deploy/README.md](deploy/README.md).

### Security

- Use a long, random token and share it only with trusted people.
- Never expose `.env`, tokens, or uploaded files.
- Use HTTPS before exposing the service to the Internet; the Nginx example is HTTP-only and does not obtain TLS certificates.
- Open only required firewall/security rules; prefer VPN/Tailscale for internal use.
- Uploaded files are not served directly by the application.

### License

This is **source-available** software, not Open Source under the OSI definition.

The source code is distributed under the [PolyForm Noncommercial License 1.0.0](LICENSE). You may use, modify, and distribute it for non-commercial purposes. Commercial use requires a separate written license. See [Commercial licensing](COMMERCIAL-LICENSE.md), [NOTICE](NOTICE), and [ENFORCEMENT.md](ENFORCEMENT.md).

Contributions are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md). For security issues, see [SECURITY.md](SECURITY.md).
