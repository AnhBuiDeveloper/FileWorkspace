# Details / Chi tiết

Extra detail moved out of the main [README](../README.md) to keep it short. / Nội dung chi tiết được tách khỏi [README](../README.md) chính để README ngắn gọn.

[English](#english) · [Tiếng Việt](#tiếng-việt)

## English

### Intended use and scope

Use this project as a private file workspace for people who already trust one another: a home lab, small internal team, private server, or VPN-protected environment. A single shared token currently grants access to browse, create folders, upload, download files or selected ZIP archives, and permanently delete selected files or folders. Protect that token accordingly.

Baseline per-IP rate limiting and a minimal security audit log ship by default (see [Rate limiting and audit logging](#rate-limiting-and-audit-logging)). For Internet-facing or multi-user use, still add a real authentication/authorization model, storage and file-size limits, malware scanning, backups, and a reverse-proxy/WAF posture appropriate to your environment.

### File operations and archive downloads

Use the checkbox beside a file or folder to build a selection, or use **Select all** for the items currently visible in the open folder. **Download ZIP** creates one streamed archive containing the selected files and folder hierarchy, including empty folders. It skips incomplete hidden `.uploading` files and avoids duplicating a file when both it and a parent folder are selected.

For an individual file, **Download** first creates an opaque GET ticket scoped to that file. The ticket works for one hour, supports Range and resume, and can be captured by IDM or another download manager. It is a bearer link: anyone who receives it can download that one file until it expires. The shared workspace token is never put in the ticket URL. Tickets exist only in server memory, so a server restart invalidates them. ZIP downloads retain their protected browser form flow.

Deleting one or more selected items always asks for confirmation. A confirmed deletion is permanent: selected folders are removed recursively with their contents, and there is no recycle bin, restore action, or audit trail. A folder containing an incomplete upload cannot be deleted until that upload is stopped or completed.

### Rate limiting and audit logging

Every `/api/*` request is throttled per client IP under one of two policies:

- **`api`** (list, delete, folder create, download/download-ticket, archive ZIP): 300 requests/minute per IP, no queue — once the ceiling is hit, the server replies `429 Too Many Requests` immediately instead of holding the request open. Sized for click-driven UI actions; it also blunts token/ticket-guessing scripts.
- **`api-uploads`** (upload session start/resume/cancel, chunk PUT): a token-bucket allowing bursts up to 2000 requests plus a steady 500/second. This absorbs "Upload folder" firing hundreds of session-start calls at once and continuous chunk streaming on fast networks, while still capping a runaway/scripted client.

A minimal audit log (`ILogger`, not a database) records `action`, `path`, and client IP for delete, download, ZIP-archive, and download-ticket-issuance requests — never the upload token or a ticket value, since a ticket is itself a one-hour bearer credential.

Both the rate limiter's per-IP partitioning and HSTS depend on seeing the real client IP/scheme, so the app honors `X-Forwarded-For`/`X-Forwarded-Proto` from a reverse proxy (the sample Nginx config already sets both).

### Operational notes

- A visible `.uploading` file means that an upload is still being written. It is removed or renamed only when the request completes.
- Pause/Resume retains progress while the browser page and server process remain available. After reload, browser close, server restart, or deployment, open the original destination folder and select the same local file again; File Workspace validates the saved session, name, size, and destination before resuming remaining chunks. Browsers cannot retain access to a local file after closing, so reselecting it is required.
- Stop immediately cancels an upload and removes its activity card. A stopped task never returns when a later upload begins; the browser requests cleanup for its incomplete hidden upload session.
- Incomplete-upload manifests and temporary `.uploading` files are hidden from the workspace. They expire after seven days and are cleaned at startup and hourly. Legacy orphan `.uploading` files created before resumable sessions cannot be resumed; they are cleaned after the same retention period.
- ZIP archives are generated as a streamed response and are not retained in `Upload/`. Keep sufficient storage for the source files and ensure the proxy does not buffer the archive response.
- Deletion is irreversible. Back up data you need to retain; the application has no recycle bin or restore operation.
- The browser remembers the upload token on the current device until **Log out**. Do not use this option on a shared browser profile; log out when finished.
- A one-file download ticket is valid for one hour. Treat its URL as sensitive until it expires; it grants download access to that file only. Server restart invalidates all outstanding tickets.
- Store uploaded files outside `wwwroot`; this project already does so.
- Use HTTPS before exposing the service on the public Internet. The included Nginx example is HTTP-only and does not issue certificates.
- Limit network exposure to trusted users or networks. A token is an access control, not a complete perimeter.

### Quality checks

The repository maintains three automated test layers:

- Unit tests for token validation, resumable upload persistence, cleanup, and file-manager storage behavior.
- API integration tests that exercise the real ASP.NET Core routes in an isolated workspace, including resumable uploads, one-hour download tickets, and ranged downloads.
- Playwright UI tests in Chromium for token persistence, localization, folder creation, upload resume after reload, upload flow, one-click Stop cleanup, selected ZIP downloads, and confirmed file deletion.

The GitHub Actions workflow runs architecture/format validation, the .NET suite, and the Playwright suite on pushes and pull requests. Update the relevant tests whenever behavior changes.

---

## Tiếng Việt

### Mục đích sử dụng và phạm vi

Project phù hợp làm không gian file riêng cho những người đã tin cậy nhau: home lab, nhóm nội bộ nhỏ, private server hoặc môi trường có VPN. Một shared token hiện cấp quyền duyệt file, tạo folder, upload, tải file hoặc ZIP các mục đã chọn và xóa vĩnh viễn file/folder đã chọn; cần bảo vệ token tương ứng.

Rate limit theo IP và audit log tối thiểu đã có sẵn mặc định (xem [Rate limit và audit log](#rate-limit-và-audit-log)). Nếu public Internet hoặc phục vụ nhiều người dùng, vẫn cần bổ sung mô hình đăng nhập/phân quyền thật sự, giới hạn dung lượng lưu trữ/kích thước file, quét mã độc, backup và reverse proxy/WAF phù hợp với môi trường.

### Thao tác file và tải archive

Dùng checkbox cạnh mỗi file/folder để chọn nhiều mục, hoặc **Chọn tất cả** các mục đang hiển thị trong folder hiện tại. **Tải ZIP** tạo một archive stream gồm file và cấu trúc folder đã chọn, kể cả folder rỗng. Archive bỏ qua file `.uploading` đang ẩn và không lặp file khi đồng thời chọn file đó cùng folder cha.

Với một file, nút **Tải xuống** tạo GET ticket opaque chỉ scope file đó. Ticket hiệu lực một giờ, hỗ trợ Range/resume và có thể để IDM hoặc download manager khác bắt link. Đây là bearer link: ai có URL đều tải được đúng file đó đến khi ticket hết hạn. Shared workspace token không nằm trong URL ticket. Ticket chỉ nằm trong memory của server nên server restart sẽ làm ticket hiện có mất hiệu lực. Tải ZIP vẫn giữ protected browser form flow.

Xóa một hoặc nhiều mục đã chọn luôn yêu cầu xác nhận. Sau khi xác nhận, các folder được chọn sẽ bị xóa đệ quy cùng toàn bộ nội dung; thao tác là vĩnh viễn vì hiện chưa có thùng rác, khôi phục hay audit trail. Không thể xóa folder đang có upload chưa hoàn tất cho đến khi upload đó dừng hoặc hoàn tất.

### Rate limit và audit log

Mọi request `/api/*` bị giới hạn tốc độ theo IP client, thuộc một trong hai policy:

- **`api`** (list, xóa, tạo folder, tải file/ticket, tải ZIP): 300 request/phút mỗi IP, không hàng đợi — vượt ngưỡng là server trả `429 Too Many Requests` ngay, không giữ request chờ. Mức này phù hợp thao tác click trên UI; đồng thời cản bớt script dò token/ticket.
- **`api-uploads`** (start/resume/cancel session upload, PUT chunk): token-bucket cho phép burst tới 2000 request cộng 500/giây đều đặn. Mức này đủ hấp thụ việc "Upload folder" bắn hàng trăm request start session cùng lúc và luồng chunk upload liên tục trên mạng nhanh, đồng thời vẫn giới hạn client chạy script bất thường.

Audit log tối thiểu (`ILogger`, không phải database) ghi lại `action`, `path` và IP client cho các request xóa, tải file, tải ZIP và cấp ticket tải — không bao giờ ghi upload token hay giá trị ticket, vì ticket tự nó là bearer credential hiệu lực một giờ.

Cả việc phân vùng theo IP của rate limiter lẫn HSTS đều cần biết đúng IP/scheme thật của client, nên app đọc `X-Forwarded-For`/`X-Forwarded-Proto` từ reverse proxy (Nginx sample sẵn có đã set cả hai).

### Lưu ý vận hành

- File `.uploading` đang hiển thị nghĩa là upload vẫn được ghi. Nó chỉ được xóa hoặc đổi tên khi request hoàn tất.
- Pause/Resume giữ tiến độ khi trang trình duyệt và process server vẫn đang hoạt động. Sau reload, đóng browser, restart server hoặc deploy, mở đúng folder đích ban đầu và chọn lại cùng local file; File Workspace sẽ validate session đã lưu, tên, dung lượng và folder đích rồi tiếp tục các chunk còn thiếu. Browser không thể giữ quyền đọc local file sau khi đóng nên cần chọn lại file.
- Stop hủy upload và xóa activity card ngay. Task đã dừng không được xuất hiện lại khi bắt đầu upload khác; browser yêu cầu server dọn phiên upload ẩn chưa hoàn tất.
- Manifest upload chưa hoàn tất và file tạm `.uploading` bị ẩn khỏi workspace. Chúng hết hạn sau bảy ngày, được dọn lúc startup và mỗi giờ. File `.uploading` mồ côi từ phiên bản trước chưa có manifest không thể resume; chúng được dọn theo cùng thời hạn.
- ZIP được tạo dưới dạng response stream và không được lưu lại trong `Upload/`. Cần giữ đủ dung lượng cho các file nguồn và bảo đảm proxy không buffer response archive.
- Xóa file/folder là không thể hoàn tác. Hãy backup dữ liệu cần giữ; ứng dụng chưa có thùng rác hoặc khôi phục.
- Trình duyệt ghi nhớ upload token trên thiết bị hiện tại đến khi **Đăng xuất**. Không dùng trên browser profile dùng chung; hãy đăng xuất khi hoàn tất.
- Ticket tải một file có hiệu lực một giờ. Hãy coi URL ticket là nhạy cảm đến khi hết hạn; nó chỉ cấp quyền tải file đó. Server restart làm toàn bộ ticket đang có mất hiệu lực.
- Lưu file upload ngoài `wwwroot`; project này đã áp dụng nguyên tắc đó.
- Dùng HTTPS trước khi public service. Nginx sample chỉ chạy HTTP và không tự cấp certificate.
- Giới hạn truy cập mạng cho người hoặc mạng tin cậy. Token là lớp kiểm soát truy cập, không phải toàn bộ lớp phòng thủ.

### Kiểm tra chất lượng

Repository duy trì ba tầng automated test:

- Unit test cho token validation, persistence/cleanup upload resume và hành vi lưu trữ của file manager.
- API integration test chạy route ASP.NET Core thật trong workspace cô lập, gồm upload resume, ticket tải một giờ và tải theo range.
- Playwright UI test trên Chromium cho lưu token, đổi ngôn ngữ, tạo folder, resume upload sau reload, upload, kiểm tra Stop một lần, tải ZIP các mục đã chọn và xóa file có xác nhận.

GitHub Actions chạy kiểm tra kiến trúc/format, .NET suite và Playwright suite cho push/pull request. Khi thay đổi behavior, phải cập nhật test liên quan.
