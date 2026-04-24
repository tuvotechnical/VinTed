# Hướng dẫn Phát triển (Developer Guide)

Tài liệu này tổng hợp các lệnh và quy trình tự động hóa (CI/CD) trong quá trình phát triển Add-in **VinTed**. Mọi thao tác đều được thực hiện thông qua **PowerShell**, KHÔNG cần sử dụng Visual Studio.

---

## 1. Môi trường yêu cầu
- **Build Engine:** `MSBuild.exe` (có sẵn trong Windows tại `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\`).
- **GitHub CLI (`gh`):** Khuyên dùng để tạo Release tự động. (Tải tại https://cli.github.com/ và chạy lệnh `gh auth login` để đăng nhập).

---

## 2. Các Script Tiện ích

Có 2 kịch bản chính khi phát triển:
1. **Lập trình và test liên tục:** Chạy `build.ps1`
2. **Ra mắt phiên bản mới:** Chạy `commit.ps1`

### 2.1. Build nội bộ (`build.ps1`)
Dùng khi bạn đang code tính năng và muốn build nhanh để test thử trong Inventor mà không tạo version mới hay up lên GitHub.

```powershell
# Chạy trong PowerShell tại thư mục dự án
.\build.ps1
```

**Quá trình xử lý:**
1. Đọc `version.json` và inject vào `AssemblyInfo.cs`.
2. Dọn dẹp thư mục `bin/Release` cũ.
3. Chạy `MSBuild.exe` để biên dịch dự án (.dll).
4. Dọn dẹp các tệp DLL hệ thống thừa và các thư mục ngôn ngữ (`af-ZA`, `es-ES`...) của ModernWpf để giảm dung lượng file ZIP.
5. Tự động tạo file manifest `VinTed.addin`.
6. Copy DLLs vào thư mục `%AppData%\Autodesk\ApplicationPlugins\VinTed` để test.

> **Lưu ý:** Script sẽ hỏi để đóng Inventor nếu đang mở (để có thể ghi đè DLL). 

### 2.2. Commit và Release tự động (`commit.ps1`)
Dùng khi bạn đã hoàn thiện tính năng và muốn **phát hành phiên bản mới (Release)**. Lệnh này đóng gói tất cả mọi thứ chỉ với 1 cú click.

**Các lệnh thường dùng:**

- **Phát hành bản sửa lỗi nhỏ (Patch Update) - Tự động tăng số cuối (VD: 1.0.0 -> 1.0.1)**:
  ```powershell
  .\commit.ps1 -Message "Fix lỗi hiển thị text" -BumpType patch
  ```
  *(Nếu không truyền `-BumpType`, mặc định script sẽ dùng `patch`)*

- **Phát hành tính năng mới (Minor Update) - Tự động tăng số giữa (VD: 1.0.1 -> 1.1.0)**:
  ```powershell
  .\commit.ps1 -Message "Thêm tính năng Auto-Dimension" -BumpType minor
  ```

- **Phát hành bản nâng cấp lớn (Major Update) - Tự động tăng số đầu (VD: 1.1.0 -> 2.0.0)**:
  ```powershell
  .\commit.ps1 -Message "Đại tu kiến trúc giao diện" -BumpType major
  ```

**Quá trình xử lý (8 bước):**
1. **Tăng Version**: Đọc `version.json` và tự động tăng số version lên theo chuẩn SemVer (Semantic Versioning).
2. **Cập nhật Version**: Lưu phiên bản mới lại vào `version.json`.
3. **Build Code**: Gọi `build.ps1` để biên dịch Add-in với số version mới nhất.
4. **Nén ZIP**: Nén thư mục cài đặt thành file `VinTed-vX.Y.Z.zip` chứa đầy đủ file cài đặt.
5. **Git Add & Commit**: Commit các thay đổi với câu mô tả `-Message`.
6. **Git Push**: Đẩy code lên nhánh `main` trên GitHub.
7. **Tạo GitHub Release**: Dùng GitHub CLI (`gh`) để public bản Release và đính kèm file nén ZIP.
8. **Dọn dẹp**: Xoá file ZIP tạm.

---

## 3. Kiến trúc Cập nhật Tự động (Auto-Update)

1. Mọi thông tin về version được điều phối bởi tệp `version.json`. Đừng chỉnh sửa phiên bản bằng tay ở các nơi khác.
2. File `install.ps1` trên Github được thiết kế để luôn luôn fetch API GitHub Release (`/releases/latest`) và lấy file `.zip` mới nhất về cài đặt tự động bằng 1 lệnh duy nhất.
3. Khi Inventor khởi động và tải Add-in, lớp `UpdateChecker` sẽ gọi API ngầm (trên một Thread khác, không gây đơ ứng dụng) để check xem GitHub Release mới nhất có lớn hơn phiên bản hiện tại hay không. Nếu có, cửa sổ WPF `UpdateNotificationWindow` sẽ hiện lên thông báo.

---

## 4. Tóm tắt luồng công việc lý tưởng

1. Viết code mới...
2. Nhấn `.\build.ps1` để debug/test locally.
3. Kiểm tra mọi thứ ok, cập nhật `README.md` nếu cần.
4. Chạy `.\commit.ps1 -Message "Hoàn thành chức năng XYZ"`
5. Done. Code đã lên GitHub, Release đã được tạo, và máy khách hàng dùng Add-in sẽ tự nhận thông báo báo có bản update!
