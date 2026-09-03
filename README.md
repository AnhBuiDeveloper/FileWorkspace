# File Upload

Web upload file, stream trực tiếp xuống server. Có upload token, progress, phần trăm và tốc độ.

## License

Đây là **source-available**, không phải Open Source theo định nghĩa OSI.

Mã nguồn phát hành theo [PolyForm Noncommercial License 1.0.0](LICENSE). Bạn có thể dùng, sửa và chia sẻ cho mục đích phi thương mại. Mọi mục đích thương mại cần thỏa thuận license riêng trước. Xem [NOTICE](NOTICE) và [Commercial licensing](COMMERCIAL-LICENSE.md).

## Development

1. Sao chép `.env.example` thành `.env`.
2. Đặt `UPLOAD_ACCESS_TOKEN` đủ mạnh.
3. Chạy `./Start-Server.ps1` trên Windows, hoặc theo tài liệu trong `deploy/` trên Ubuntu ARM64.

Không commit `.env`, token, thư mục `Upload/`, hoặc file upload thật.

## Contribute

Pull request hoan nghênh. Xem [CONTRIBUTING.md](CONTRIBUTING.md). Bằng cách đóng góp, bạn cấp quyền cho dự án dùng contribution theo cùng license.

## Reporting misuse

Nếu thấy dự án bị dùng thương mại trái phép, đừng tranh cãi công khai. Lưu bằng chứng và báo cho chủ sở hữu qua GitHub repository. Xem quy trình tại [ENFORCEMENT.md](ENFORCEMENT.md).
