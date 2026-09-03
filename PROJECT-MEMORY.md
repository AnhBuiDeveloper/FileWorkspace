# Project memory / Bộ nhớ dự án

This document is the durable engineering context for File Workspace, a self-hosted file manager. Read it before changing application behavior, API contracts, storage, or user interface.

## English

### Engineering principles

- **SOLID:** give each component one clear responsibility. Keep HTTP concerns in endpoints, application/file-system behavior in services, and request/response shapes in models.
- **KISS:** choose the smallest understandable solution that satisfies the current requirement. Prefer standard ASP.NET Core and browser features over new dependencies.
- **DRY:** centralize repeated validation, token checks, error mapping, path handling, and UI strings. Do not duplicate business rules across endpoints or client code.
- **YAGNI:** do not add roles, databases, queues, cloud integrations, abstractions, configuration options, or extension points until a concrete accepted requirement needs them.

### Current architecture

| Area | Responsibility |
| --- | --- |
| Program.cs | Application composition, middleware, and dependency registration only. |
| Configuration/ | Local development environment-file loading. |
| Endpoints/ | HTTP routes, authentication boundary, request parsing, and HTTP responses. |
| Services/UploadTokenValidator | Constant-time upload-token comparison. |
| Services/FileManagerService | Upload sessions/chunks, safe paths, folders, file listing, downloads, and disk persistence. |
| Models/ | API request/response contracts and upload protocol constants. |
| wwwroot/ | Browser-only presentation and interaction. |

### Rules for future changes

1. Preserve token protection for every file, folder, upload, and download operation.
2. Treat every client-supplied path or filename as untrusted. Resolve paths only through the file manager service.
3. Keep incomplete uploads hidden and never serve Upload/ as static files.
4. Keep API changes backward-compatible unless a versioned breaking change is explicitly approved.
5. Follow [UI-STANDARDS.md](UI-STANDARDS.md) for every browser-facing change.
6. Update and run the relevant automated tests before committing. Maintain unit, API integration, and Playwright UI coverage for behavior changes; update docs when behavior or configuration changes.
7. Describe the product accurately as a self-hosted file manager. Do not imply multi-user cloud-drive capabilities unless they are implemented and documented.

### Automated quality guardrails

- Run \`npm run test:architecture\` before every pull request. It verifies the current architecture boundaries, minimal composition, centralized endpoint behavior, baseline tests, and durable engineering/UI standards.
- Run \`dotnet format --verify-no-changes --no-restore\` to keep the C# codebase consistently formatted.
- These checks enforce objective guardrails, not a substitute for human design review. Reviewers must still assess SOLID, KISS, DRY, and YAGNI against the accepted requirement.

## Tiếng Việt

### Nguyên tắc kỹ thuật

- **SOLID:** mỗi component có một trách nhiệm rõ ràng. HTTP ở endpoint, nghiệp vụ/file-system ở service, request/response ở model.
- **KISS:** chọn giải pháp nhỏ nhất, dễ hiểu nhất đáp ứng yêu cầu hiện tại. Ưu tiên tính năng chuẩn của ASP.NET Core và browser thay vì thêm dependency.
- **DRY:** tập trung validation, token check, error mapping, path handling và UI string. Không lặp business rule ở endpoint hoặc client.
- **YAGNI:** không thêm role, database, queue, cloud integration, abstraction, option cấu hình hoặc extension point khi chưa có yêu cầu cụ thể được chấp thuận.

### Kiến trúc hiện tại

| Khu vực | Trách nhiệm |
| --- | --- |
| Program.cs | Chỉ composition ứng dụng, middleware và đăng ký dependency. |
| Configuration/ | Nạp file môi trường local. |
| Endpoints/ | HTTP route, ranh giới xác thực, parse request và HTTP response. |
| Services/UploadTokenValidator | So sánh upload token theo constant-time. |
| Services/FileManagerService | Upload session/chunk, path an toàn, folder, list file, download và ghi ổ đĩa. |
| Models/ | Contract request/response API và hằng số upload protocol. |
| wwwroot/ | Presentation và interaction chỉ chạy trên browser. |

### Quy tắc cho thay đổi sau này

1. Giữ token protection cho mọi thao tác file, folder, upload và download.
2. Coi mọi path/tên file từ client là không tin cậy. Chỉ resolve path qua file manager service.
3. Giữ upload chưa hoàn tất ở trạng thái ẩn và không bao giờ serve Upload/ như static file.
4. Giữ API backward-compatible trừ khi breaking change có version được phê duyệt rõ ràng.
5. Tuân thủ [UI-STANDARDS.md](UI-STANDARDS.md) với mọi thay đổi browser-facing.
6. Cập nhật và chạy automated test phù hợp trước khi commit. Duy trì coverage unit, API integration và Playwright UI cho thay đổi hành vi; cập nhật docs khi behavior hoặc config thay đổi.
7. Mô tả chính xác sản phẩm là file manager tự host. Không ngụ ý tính năng cloud drive đa người dùng khi chưa được triển khai và ghi tài liệu.

### Guardrail chất lượng tự động

- Chạy \`npm run test:architecture\` trước mỗi pull request. Check này xác minh boundary kiến trúc hiện tại, composition tối giản, endpoint behavior được tập trung, baseline test và tiêu chuẩn kỹ thuật/UI lâu dài.
- Chạy \`dotnet format --verify-no-changes --no-restore\` để giữ định dạng C# nhất quán.
- Đây là guardrail có tiêu chí khách quan, không thay thế design review của con người. Reviewer vẫn phải đánh giá SOLID, KISS, DRY và YAGNI theo yêu cầu đã được chấp thuận.
