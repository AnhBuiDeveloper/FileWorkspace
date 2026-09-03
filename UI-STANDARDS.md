# Responsive UI standard / Tiêu chuẩn UI responsive

This standard applies to every user-interface change in this repository. A pull request that changes HTML, CSS, or browser-facing JavaScript must meet these requirements before merge.

## English

### Required viewport checks

Review the changed interface at least at these CSS viewport widths:

| Device class | Width |
| --- | ---: |
| Small phone | 320 px |
| Typical phone | 375 px |
| Large phone | 430 px |
| Tablet | 768 px |
| Laptop / desktop | 1024 px |
| Wide desktop | 1440 px |

Check both portrait and landscape when the change affects mobile layout.

### Acceptance criteria

- No horizontal page scrolling at any required viewport.
- Text, long filenames, status messages, and error messages remain readable without overlapping controls.
- Primary actions remain visible and usable; lists scroll within their own region when content is long.
- Touch controls have a practical target size of at least 40 × 40 CSS pixels; use 44 × 44 where space permits.
- Controls can be reached and operated with keyboard only. Focus must remain visible.
- Layout must work with browser zoom at 200% and with increased text size where supported.
- The interface must respect `prefers-reduced-motion`; animation must not be required to understand state.
- Do not rely on hover as the only way to reveal a required action or status.
- Use fluid sizes (`min()`, `max()`, `clamp()`, flex/grid) before adding device-specific breakpoints.
- Add or update localized text for both English and Vietnamese when user-visible wording changes.

### Review checklist

Before requesting review, record the tested viewport widths in the pull request and confirm:

```text
- [ ] No horizontal overflow
- [ ] Long content is handled
- [ ] Touch and keyboard interaction work
- [ ] English and Vietnamese reviewed
- [ ] Reduced-motion behavior reviewed
```

## Tiếng Việt

Tiêu chuẩn này áp dụng cho mọi thay đổi giao diện trong repository. Pull request thay đổi HTML, CSS hoặc JavaScript chạy trên trình duyệt phải đạt các yêu cầu sau trước khi merge.

### Viewport bắt buộc kiểm tra

Kiểm tra giao diện đã thay đổi ít nhất tại các chiều rộng CSS sau:

| Nhóm thiết bị | Chiều rộng |
| --- | ---: |
| Điện thoại nhỏ | 320 px |
| Điện thoại phổ biến | 375 px |
| Điện thoại lớn | 430 px |
| Tablet | 768 px |
| Laptop / desktop | 1024 px |
| Desktop rộng | 1440 px |

Kiểm tra cả dọc và ngang nếu thay đổi tác động đến layout mobile.

### Tiêu chí đạt

- Không xuất hiện cuộn ngang toàn trang ở bất kỳ viewport bắt buộc nào.
- Nội dung dài như tên file, trạng thái và lỗi vẫn đọc được, không đè lên control.
- Thao tác chính luôn thấy và dùng được; danh sách có vùng cuộn riêng khi nội dung dài.
- Control cảm ứng có vùng chạm thực tế ít nhất 40 × 40 CSS pixel; ưu tiên 44 × 44 khi đủ chỗ.
- Có thể dùng toàn bộ control bằng bàn phím; focus phải hiển thị rõ.
- Layout hoạt động ở browser zoom 200% và khi tăng cỡ chữ nếu nền tảng hỗ trợ.
- Tôn trọng `prefers-reduced-motion`; không được phụ thuộc animation để hiểu trạng thái.
- Không dùng hover là cách duy nhất để thấy action hoặc trạng thái cần thiết.
- Ưu tiên kích thước linh hoạt (`min()`, `max()`, `clamp()`, flex/grid) trước khi thêm breakpoint theo thiết bị.
- Cập nhật văn bản hiển thị cho cả English và Tiếng Việt khi thay đổi wording.

### Checklist review

Trước khi yêu cầu review, ghi các viewport đã kiểm tra trong pull request và xác nhận:

```text
- [ ] Không tràn ngang
- [ ] Xử lý được nội dung dài
- [ ] Dùng được bằng cảm ứng và bàn phím
- [ ] Đã kiểm tra English và Tiếng Việt
- [ ] Đã kiểm tra reduced motion
```
