# File Workspace — Self-hosted File Manager

> A private, token-protected file workspace for trusted users and infrastructure you control.

[English](#english) · [Tiếng Việt](#tiếng-việt)

## English

File Workspace is a self-hosted, cloud-style file manager built with ASP.NET Core. It gives a trusted group a simple browser workspace to organize folders, upload large files, and download stored files from storage that you control. Uploads stream directly to disk rather than being buffered in memory, while the browser reports progress, transferred bytes, and current speed.

It is deliberately small and self-contained. Today it is not a multi-tenant cloud drive: it has no user accounts, roles, sharing links, client sync, quotas, audit trail, remote object storage, or antivirus integration.

### Highlights

- Browse the file workspace, navigate folders, create folders, and download files.
- Upload multiple files through selection or drag and drop, including an entire local folder.
- Upload to the currently open folder, with independent per-file progress, speed, pause, resume, and stop controls.
- Stream large files directly to disk with resumable chunk uploads.
- Keep incomplete uploads as hidden .uploading files, then atomically rename them only after a successful upload.
- Protect all file-manager actions with one access token supplied through UPLOAD_ACCESS_TOKEN.
- Provide English and Vietnamese interfaces; remember language and token locally until the user logs out.
- Render responsively across desktop, tablet, and small touch screens.

### Intended use and scope

Use this project as a private file workspace for people who already trust one another: a home lab, small internal team, private server, or VPN-protected environment. A single shared token currently grants access to browse, create folders, upload, and download. Protect that token accordingly.

For Internet-facing or multi-user use, add an authentication model, authorization, HTTPS, storage and file-size limits, rate limiting, malware scanning, auditing, backups, and a reverse-proxy/WAF posture appropriate to your environment.

### Requirements

- .NET SDK 10 to build or run from source. Use ASP.NET Core Runtime 10 for published output.
- A writable `Upload/` directory in the application's content root.
- A value for `UPLOAD_ACCESS_TOKEN`. The service intentionally refuses to start without one.

### Quick start

Create `.env` in the repository root:

```env
UPLOAD_ACCESS_TOKEN=replace-with-a-long-random-secret
```

On Windows, run the convenience script:

```powershell
./Start-Server.ps1
```

Or run from source on any supported platform:

```text
dotnet run --urls http://127.0.0.1:5088
```

Open `http://127.0.0.1:5088`, enter the upload token, and choose a file. The `.env` file is ignored by Git; never commit it.

### Deployment

You may host the service on any environment that supports ASP.NET Core 10: Windows, Linux on x64 or ARM64, containers, virtual machines, bare metal, or a managed cloud platform.

After publishing for your target runtime, provide the token through an environment variable, your platform's secret manager, or a `.env` file beside `FileWorkspace.dll`, then run:

```text
dotnet FileWorkspace.dll --urls http://127.0.0.1:5088
```

The [`deploy/`](deploy/README.md) directory contains an optional Linux systemd + Nginx reference configuration. It is an example, not a hosting requirement. Adapt its paths, service account, domain, ports, and TLS setup to your own environment.

### Operational notes

- A visible `.uploading` file means that an upload is still being written. It is removed or renamed only when the request completes.
- Pause/Resume retains progress while the browser page and server process remain available. Reloading the page or restarting the server starts a new upload session.
- The browser remembers the upload token on the current device until **Log out**. Do not use this option on a shared browser profile; log out when finished.
- Store uploaded files outside `wwwroot`; this project already does so.
- Use HTTPS before exposing the service on the public Internet. The included Nginx example is HTTP-only and does not issue certificates.
- Limit network exposure to trusted users or networks. A token is an access control, not a complete perimeter.

### Quality checks

The repository maintains three automated test layers:

- Unit tests for token validation and file-manager storage behavior.
- API integration tests that exercise the real ASP.NET Core routes in an isolated workspace.
- Playwright UI tests in Chromium for token persistence, localization, folder creation, and upload flow.

Run all checks locally:

~~~text
dotnet test FileWorkspace.Tests/FileWorkspace.Tests.csproj --configuration Release
npm ci
npx playwright install chromium
npm run test:ui
~~~

The GitHub Actions workflow runs the .NET suite and Playwright suite on pushes and pull requests. Update the relevant tests whenever behavior changes.

### Security, contributions, and licensing

Read [SECURITY.md](SECURITY.md) before reporting a vulnerability, and [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.
UI contributions must also meet the [Responsive UI standard](UI-STANDARDS.md).
Architecture and engineering rules are maintained in [PROJECT-MEMORY.md](PROJECT-MEMORY.md).

This project is **source-available**, not Open Source under the OSI definition. It is licensed under the [PolyForm Noncommercial License 1.0.0](LICENSE). Non-commercial use, modification, and distribution are permitted; commercial use requires a separate written agreement. See [COMMERCIAL-LICENSE.md](COMMERCIAL-LICENSE.md), [NOTICE](NOTICE), and [ENFORCEMENT.md](ENFORCEMENT.md).

---

## Tiếng Việt

File Workspace là file manager tự host theo hướng cloud-style, xây dựng bằng ASP.NET Core. Ứng dụng cung cấp không gian web đơn giản để nhóm người dùng tin cậy tổ chức folder, upload file lớn và tải file từ nơi lưu trữ do chính bạn kiểm soát. Dữ liệu được stream thẳng xuống ổ đĩa, không giữ toàn bộ file trong RAM; trình duyệt hiển thị tiến độ, dung lượng đã gửi và tốc độ hiện tại.

Project được chủ đích giữ gọn và độc lập. Hiện tại đây chưa phải cloud drive đa người dùng: chưa có tài khoản, role, link chia sẻ, client đồng bộ, quota, audit trail, object storage từ xa hoặc tích hợp antivirus.

### Điểm nổi bật

- Duyệt không gian file, đi vào folder, tạo folder và tải xuống file.
- Upload nhiều file bằng chọn file hoặc kéo-thả, gồm cả một local folder.
- Upload vào folder đang mở; mỗi file có tiến độ, tốc độ, pause, resume và stop độc lập.
- Stream file lớn trực tiếp xuống ổ đĩa bằng upload theo chunk có thể resume.
- Giữ upload chưa hoàn tất ở dạng file .uploading ẩn; chỉ đổi tên nguyên tử khi upload thành công.
- Bảo vệ mọi thao tác file manager bằng một access token từ UPLOAD_ACCESS_TOKEN.
- Giao diện Anh và Việt; trình duyệt ghi nhớ ngôn ngữ và token cục bộ đến khi người dùng đăng xuất.
- Layout responsive cho desktop, tablet và màn hình cảm ứng nhỏ.

### Mục đích sử dụng và phạm vi

Project phù hợp làm không gian file riêng cho những người đã tin cậy nhau: home lab, nhóm nội bộ nhỏ, private server hoặc môi trường có VPN. Một shared token hiện cấp quyền duyệt file, tạo folder, upload và download; cần bảo vệ token tương ứng.

Nếu public Internet hoặc phục vụ nhiều người dùng, hãy bổ sung mô hình đăng nhập và phân quyền, HTTPS, giới hạn dung lượng lưu trữ/kích thước file, rate limit, quét mã độc, audit, backup và reverse proxy/WAF phù hợp với môi trường.

### Yêu cầu

- .NET SDK 10 để build hoặc chạy từ source; ASP.NET Core Runtime 10 để chạy bản đã publish.
- Process chạy ứng dụng cần quyền ghi vào thư mục `Upload/` trong content root.
- Bắt buộc có `UPLOAD_ACCESS_TOKEN`; service sẽ không khởi động nếu thiếu token.

### Khởi chạy nhanh

Tạo file `.env` ở thư mục gốc repository:

```env
UPLOAD_ACCESS_TOKEN=thay-bang-token-dai-va-ngau-nhien
```

Trên Windows, chạy script tiện ích:

```powershell
./Start-Server.ps1
```

Hoặc chạy từ source trên mọi nền tảng được .NET hỗ trợ:

```text
dotnet run --urls http://127.0.0.1:5088
```

Mở `http://127.0.0.1:5088`, nhập token upload và chọn file. `.env` đã được Git bỏ qua; tuyệt đối không commit file này.

### Triển khai

Bạn có thể host service ở bất kỳ môi trường nào hỗ trợ ASP.NET Core 10: Windows, Linux x64/ARM64, container, máy ảo, bare metal hoặc cloud platform.

Sau khi publish cho đúng runtime đích, đặt token qua biến môi trường, secret manager của nền tảng hoặc file `.env` cạnh `FileWorkspace.dll`, rồi chạy:

```text
dotnet FileWorkspace.dll --urls http://127.0.0.1:5088
```

Thư mục [`deploy/`](deploy/README.md) có cấu hình tham khảo Linux systemd + Nginx. Đây chỉ là ví dụ, không phải yêu cầu về nơi host. Hãy điều chỉnh path, service account, domain, port và TLS cho môi trường của bạn.

### Lưu ý vận hành

- File `.uploading` đang hiển thị nghĩa là upload vẫn được ghi. Nó chỉ được xóa hoặc đổi tên khi request hoàn tất.
- Pause/Resume giữ tiến độ khi trang trình duyệt và process server vẫn đang hoạt động. Reload trang hoặc restart server sẽ tạo một phiên upload mới.
- Trình duyệt ghi nhớ upload token trên thiết bị hiện tại đến khi **Đăng xuất**. Không dùng trên browser profile dùng chung; hãy đăng xuất khi hoàn tất.
- Lưu file upload ngoài `wwwroot`; project này đã áp dụng nguyên tắc đó.
- Dùng HTTPS trước khi public service. Nginx sample chỉ chạy HTTP và không tự cấp certificate.
- Giới hạn truy cập mạng cho người hoặc mạng tin cậy. Token là lớp kiểm soát truy cập, không phải toàn bộ lớp phòng thủ.

### Kiểm tra chất lượng

Repository duy trì ba tầng automated test:

- Unit test cho token validation và hành vi lưu trữ của file manager.
- API integration test chạy route ASP.NET Core thật trong workspace cô lập.
- Playwright UI test trên Chromium cho lưu token, đổi ngôn ngữ, tạo folder và luồng upload.

Chạy toàn bộ kiểm tra tại local:

~~~text
dotnet test FileWorkspace.Tests/FileWorkspace.Tests.csproj --configuration Release
npm ci
npx playwright install chromium
npm run test:ui
~~~

GitHub Actions chạy .NET suite và Playwright suite cho push/pull request. Khi thay đổi behavior, phải cập nhật test liên quan.

### Bảo mật, đóng góp và license

Đọc [SECURITY.md](SECURITY.md) trước khi báo lỗ hổng và [CONTRIBUTING.md](CONTRIBUTING.md) trước khi mở pull request.
Thay đổi UI cũng phải đạt [Tiêu chuẩn UI responsive](UI-STANDARDS.md).
Kiến trúc và quy tắc kỹ thuật được lưu tại [PROJECT-MEMORY.md](PROJECT-MEMORY.md).

Đây là dự án **source-available**, không phải Open Source theo định nghĩa OSI. Mã nguồn dùng [PolyForm Noncommercial License 1.0.0](LICENSE): được dùng, sửa và phân phối cho mục đích phi thương mại; mục đích thương mại cần một thỏa thuận bằng văn bản riêng. Xem [COMMERCIAL-LICENSE.md](COMMERCIAL-LICENSE.md), [NOTICE](NOTICE) và [ENFORCEMENT.md](ENFORCEMENT.md).
