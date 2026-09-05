# Browser-local resume queue / Hàng đợi khôi phục theo browser

## English

When an upload is interrupted, File Workspace retains its hidden server upload session for seven days. The browser that started it stores only non-secret session metadata and the latest server-confirmed byte count in its local storage. After reopening the workspace, that browser shows an **Uploads ready to resume** card with its progress and a **Resume** button. Select the original local file to continue it.

Another browser profile or device has no local record, so it does not display that card and cannot discover another browser's pending uploads. The access token is never stored in the resume record. Browser storage can be cleared; in that case the server session remains hidden until it expires, but the browser no longer has a safe, browser-local way to offer it for resume.

## Tiếng Việt

Khi upload bị gián đoạn, File Workspace giữ upload session ẩn trên server trong bảy ngày. Browser đã bắt đầu upload chỉ lưu metadata không bí mật của session và số byte mới nhất server đã xác nhận trong local storage. Khi mở lại workspace, chính browser đó hiện card **Upload đang chờ khôi phục**, progress và nút **Khôi phục**. Chọn lại file local gốc để tiếp tục.

Browser profile hoặc thiết bị khác không có local record nên không thấy card và không thể biết upload dở dang của browser khác. Resume record không bao giờ chứa access token. Nếu browser storage bị xóa, session server vẫn ẩn tới khi hết hạn, nhưng browser không còn cách an toàn, theo từng browser để đề nghị resume.
