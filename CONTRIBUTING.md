# Contributing / Đóng góp

## English

Contributions to this self-hosted file manager are welcome. Before opening a pull request:

- Human and AI contributors must follow [AGENTS.md](AGENTS.md). It defines required pre-read, architecture/security constraints, validation loop, stop points.
- Do not commit tokens, `.env` files, real uploads, personal data, or credentials.
- Do not add a dependency whose license conflicts with [LICENSE](LICENSE).
- Update documentation and tests whenever behavior changes.
- Keep changes focused and explain the user-visible impact in the pull request.
- For UI changes, follow [UI-STANDARDS.md](UI-STANDARDS.md) and include its review checklist in the pull request.
- Follow [PROJECT-MEMORY.md](PROJECT-MEMORY.md) for architecture boundaries and SOLID, KISS, DRY, and YAGNI decisions.
- The default branch is protected. Create a topic branch, open a pull request, resolve every conversation, and merge only after the `dotnet-tests` and `playwright` checks pass.

### Developer Certificate of Origin

By submitting a contribution, you confirm that you have the right to submit it and grant the project permission to distribute it under the [PolyForm Noncommercial License 1.0.0](LICENSE).

Add this line to the commit message:

```text
Signed-off-by: Your Name <your.email@example.com>
```

Use:

```text
git commit -s -m "Describe the change"
```

Do not sign off on behalf of anyone else.

## Tiếng Việt

Hoan nghênh đóng góp cho file manager tự host này. Trước khi mở pull request:

- Contributor là người hoặc AI đều phải tuân thủ [AGENTS.md](AGENTS.md). Tài liệu này quy định tài liệu phải đọc trước, boundary kiến trúc/bảo mật, vòng lặp kiểm tra, điểm phải dừng xin ý kiến.
- Không commit token, file `.env`, file thật trong workspace, dữ liệu cá nhân hoặc credential.
- Không thêm dependency có license xung đột với [LICENSE](LICENSE).
- Cập nhật tài liệu và test khi thay đổi hành vi.
- Giữ thay đổi tập trung và mô tả ảnh hưởng với người dùng trong pull request.
- Với thay đổi UI, tuân thủ [UI-STANDARDS.md](UI-STANDARDS.md) và đưa checklist review của tài liệu này vào pull request.
- Tuân thủ [PROJECT-MEMORY.md](PROJECT-MEMORY.md) về boundary kiến trúc và các quyết định SOLID, KISS, DRY, YAGNI.
- Default branch đã được bảo vệ. Hãy tạo topic branch, mở pull request, giải quyết toàn bộ conversation và chỉ merge khi `dotnet-tests` cùng `playwright` pass.

### Developer Certificate of Origin

Bằng cách gửi contribution, bạn xác nhận rằng bạn có quyền gửi nó và cấp cho dự án quyền phát hành contribution đó theo [PolyForm Noncommercial License 1.0.0](LICENSE).

Thêm dòng sau vào message commit:

```text
Signed-off-by: Your Name <your.email@example.com>
```

Dùng lệnh:

```text
git commit -s -m "Mô tả thay đổi"
```

Không ký thay cho người khác.
