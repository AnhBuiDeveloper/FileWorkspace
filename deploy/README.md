# Self-hosted file manager deployment examples

[Tiếng Việt](#tiếng-việt) · [English](#english)

## Tiếng Việt

Các file trong thư mục này là mẫu triển khai file manager tự host trên Linux với systemd và Nginx. Chúng không giới hạn nơi bạn host ứng dụng. Bạn có thể chạy trên Windows, bất kỳ Linux distribution/CPU architecture có ASP.NET Core Runtime 10, container, VM, bare metal hoặc cloud platform.

### Cách chạy tối thiểu

1. Publish đúng runtime của server, hoặc copy output publish sẵn có.
2. Tạo thư mục `Upload` có quyền ghi cho process chạy ứng dụng.
3. Đặt `UPLOAD_ACCESS_TOKEN` qua biến môi trường, secret manager, hoặc `.env` cạnh file DLL.
4. Chạy `dotnet FileWorkspace.dll --urls http://127.0.0.1:5088`.
5. Nếu dùng reverse proxy, chuyển tiếp request tới cổng nội bộ này và tắt request buffering cho endpoint upload.

### Dùng systemd + Nginx sample

Trước khi dùng, sửa các giá trị hard-code trong `file-workspace.service` và `file-workspace.nginx.conf`:

- `/opt/file-workspace`: thư mục cài đặt của bạn.
- `fileworkspace`: user/group chạy service, cần quyền ghi `Upload/`.
- `127.0.0.1:5088`: cổng nội bộ; đổi nếu trùng.
- `server_name _`: domain của bạn.

Copy `.env.example` thành `.env` trong thư mục deploy và thay token. Đặt quyền đọc file token chỉ cho service account. Nginx sample chỉ phục vụ HTTP; tự cấu hình certificate/HTTPS theo domain và nhà cung cấp của bạn.

## English

The files in this directory are Linux deployment examples for the self-hosted file manager using systemd and Nginx. They do not limit where the application can be hosted. You can run it on Windows, any Linux distribution/CPU architecture supported by ASP.NET Core Runtime 10, a container, VM, bare metal, or a cloud platform.

### Minimum run path

1. Publish for the server's runtime, or copy existing published output.
2. Create an `Upload` directory writable by the application process.
3. Set `UPLOAD_ACCESS_TOKEN` through an environment variable, secret manager, or `.env` file beside the DLL.
4. Run `dotnet FileWorkspace.dll --urls http://127.0.0.1:5088`.
5. When using a reverse proxy, forward requests to that internal port and disable request buffering for the upload endpoint.

### Using the systemd + Nginx example

Before using it, replace the hard-coded values in `file-workspace.service` and `file-workspace.nginx.conf`:

- `/opt/file-workspace`: your installation directory.
- `fileworkspace`: the service user/group; it needs write access to `Upload/`.
- `127.0.0.1:5088`: the internal port; change it if occupied.
- `server_name _`: your domain name.

Copy `.env.example` to `.env` in the deployment directory and replace the token. Limit token-file read access to the service account. The Nginx example only serves HTTP; configure a certificate and HTTPS for your own domain/provider.
