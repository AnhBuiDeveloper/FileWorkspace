# File Upload

> A fast, self-hosted file upload service for trusted users and private infrastructure.

[English](#english) · [Tiếng Việt](#tiếng-việt)

## English

File Upload is a lightweight ASP.NET Core application for receiving large files through a simple browser interface. Uploads stream directly to disk rather than being buffered in memory, while the browser reports progress, transferred bytes, and current speed.

### Highlights

- Upload any file through file selection or drag and drop.
- Live progress, percentage, transferred size, and upload speed.
- Token-protected upload endpoint using `UPLOAD_ACCESS_TOKEN`.
- Direct-to-disk streaming for large uploads.
- Temporary `.uploading` files are atomically renamed after a successful upload.
- Existing filenames are preserved; uploaded files are not served by the application.

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

After publishing for your target runtime, provide the token through an environment variable, your platform's secret manager, or a `.env` file beside `FileUpload.dll`, then run:

```text
dotnet FileUpload.dll --urls http://127.0.0.1:5088
```

The [`deploy/`](deploy/README.md) directory contains an optional Linux systemd + Nginx reference configuration. It is an example, not a hosting requirement. Adapt its paths, service account, domain, ports, and TLS setup to your own environment.

### Operational notes

- A visible `.uploading` file means that an upload is still being written. It is removed or renamed only when the request completes.
- Store uploaded files outside `wwwroot`; this project already does so.
- Use HTTPS before exposing the service on the public Internet. The included Nginx example is HTTP-only and does not issue certificates.
- Limit network exposure to trusted users or networks. A token is an access control, not a complete perimeter.

### Security, contributions, and licensing

Read [SECURITY.md](SECURITY.md) before reporting a vulnerability, and [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

This project is **source-available**, not Open Source under the OSI definition. It is licensed under the [PolyForm Noncommercial License 1.0.0](LICENSE). Non-commercial use, modification, and distribution are permitted; commercial use requires a separate written agreement. See [COMMERCIAL-LICENSE.md](COMMERCIAL-LICENSE.md), [NOTICE](NOTICE), and [ENFORCEMENT.md](ENFORCEMENT.md).

---

## Tiếng Việt

File Upload là ứng dụng ASP.NET Core gọn nhẹ để nhận file dung lượng lớn qua giao diện web đơn giản. Dữ liệu được stream thẳng xuống ổ đĩa, không giữ toàn bộ file trong RAM; trình duyệt hiển thị tiến độ, dung lượng đã gửi và tốc độ hiện tại.

### Điểm nổi bật

- Upload mọi loại file bằng chọn file hoặc kéo-thả.
- Hiển thị tiến độ, phần trăm, dung lượng đã gửi và tốc độ upload theo thời gian thực.
- Endpoint upload được bảo vệ bằng token `UPLOAD_ACCESS_TOKEN`.
- Stream trực tiếp xuống ổ đĩa, phù hợp với file lớn.
- File tạm có đuôi `.uploading` chỉ được đổi tên nguyên tử sau khi upload thành công.
- Không phục vụ trực tiếp file upload; tên file đã tồn tại được giữ nguyên.

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

Sau khi publish cho đúng runtime đích, đặt token qua biến môi trường, secret manager của nền tảng hoặc file `.env` cạnh `FileUpload.dll`, rồi chạy:

```text
dotnet FileUpload.dll --urls http://127.0.0.1:5088
```

Thư mục [`deploy/`](deploy/README.md) có cấu hình tham khảo Linux systemd + Nginx. Đây chỉ là ví dụ, không phải yêu cầu về nơi host. Hãy điều chỉnh path, service account, domain, port và TLS cho môi trường của bạn.

### Lưu ý vận hành

- File `.uploading` đang hiển thị nghĩa là upload vẫn được ghi. Nó chỉ được xóa hoặc đổi tên khi request hoàn tất.
- Lưu file upload ngoài `wwwroot`; project này đã áp dụng nguyên tắc đó.
- Dùng HTTPS trước khi public service. Nginx sample chỉ chạy HTTP và không tự cấp certificate.
- Giới hạn truy cập mạng cho người hoặc mạng tin cậy. Token là lớp kiểm soát truy cập, không phải toàn bộ lớp phòng thủ.

### Bảo mật, đóng góp và license

Đọc [SECURITY.md](SECURITY.md) trước khi báo lỗ hổng và [CONTRIBUTING.md](CONTRIBUTING.md) trước khi mở pull request.

Đây là dự án **source-available**, không phải Open Source theo định nghĩa OSI. Mã nguồn dùng [PolyForm Noncommercial License 1.0.0](LICENSE): được dùng, sửa và phân phối cho mục đích phi thương mại; mục đích thương mại cần một thỏa thuận bằng văn bản riêng. Xem [COMMERCIAL-LICENSE.md](COMMERCIAL-LICENSE.md), [NOTICE](NOTICE) và [ENFORCEMENT.md](ENFORCEMENT.md).
