# VinTed — Autodesk Inventor Add-in

**VinTed** là một Add-in tùy chỉnh dành cho Autodesk Inventor, được xây dựng bằng C# (.NET Framework 4.5) và giao diện WPF. Mục tiêu của dự án là tự động hóa các tác vụ thiết kế và bản vẽ (Drawing) để tiết kiệm thời gian, với kiến trúc cài đặt ở mức người dùng (User-level) không cần quyền Admin.

---

## 1. Kiến trúc & Môi trường phát triển

* **Ngôn ngữ & Framework:** C# 5.0, .NET Framework 4.5.
* **UI Framework:** WPF + **ModernWpf** (Windows 10/11 Fluent Design, Light Theme).
* **Inventor API:** `Autodesk.Inventor.Interop.dll` (Inventor 2023, không Embed Interop Types).
* **Build Tool:** `MSBuild.exe` nội bộ (`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`).
* **Cơ chế triển khai:**
  * Biên dịch qua script PowerShell `build.ps1`.
  * Deploy tự động vào `%AppData%\Autodesk\ApplicationPlugins\VinTed`.
* **Entry Point:** Class `StandardAddInServer` (`ApplicationAddInServer`, `[ComVisible(true)]`).
  * Đăng ký Ribbon Tab **VinTed** → Panel **Text Tools** trong môi trường Drawing.
  * Tích hợp `AppDomain.AssemblyResolve` handler để Inventor CLR tìm thấy ModernWpf.dll.

---

## 2. Tính năng hiện tại

### A. Find & Replace Text (Tìm kiếm và thay thế)
* **Chức năng:** Tìm và thay thế hàng loạt nội dung Text trong môi trường Drawing.
* **Đối tượng hỗ trợ:** General Notes, Leader Notes, Title Blocks (Prompt TextBox), Sketched Symbols (Prompt TextBox).
* **Giao diện:** WPF ModernWpf **Light Theme** — Windows 10/11 Fluent Design, native window chrome, placeholder text.
* **Kỹ thuật cốt lõi:**
  * Tìm kiếm không phân biệt hoa thường (case-insensitive) sử dụng `Regex.Replace`.
  * Hỗ trợ ký tự `*` thay cho dấu cách để nhập liệu thuận tiện.
  * **An toàn bộ nhớ:** Gọi `Marshal.ReleaseComObject` cho các đối tượng COM không khớp.
  * **An toàn dữ liệu:** Tích hợp `Transaction` cho mỗi lần thay thế, hỗ trợ `Undo/Redo` (Ctrl+Z).
  * Tự động zoom đến vị trí Text tìm được (`AppZoomSelectCmd`).
  * 3 chế độ: **Find Next** (duyệt tuần tự, quay vòng), **Replace** (thay thế mục hiện tại), **Replace All** (thay thế toàn bộ).

---

## 3. Dependencies

| Thư viện | Phiên bản | Mục đích |
|----------|-----------|----------|
| `Autodesk.Inventor.Interop` | 2023 | Inventor COM API |
| `ModernWpf` | 0.9.6 (net45) | WPF Fluent Design UI |
| `ModernWpf.Controls` | 0.9.6 (net45) | Extended WPF controls |
| `stdole` | (Inventor Bin) | `IPictureDisp` cho icon |
| `System.Windows.Forms` | (.NET) | `AxHost` icon conversion |

---

## 4. Hướng dẫn Build

Mở **PowerShell** tại thư mục dự án và chạy:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

**Quá trình script thực hiện:**
1. Đóng tiến trình Inventor (nếu đang chạy) để giải phóng file.
2. Dọn dẹp thư mục Build (`bin\Release`).
3. Gọi `MSBuild` để compile `VinTed.csproj` (Release).
4. Dọn dẹp DLL hệ thống (`mscorlib.dll`, `.nlp`) — tránh crash Inventor.
5. Copy `ModernWpf.dll` + `ModernWpf.Controls.dll` vào output.
6. Tạo/cập nhật file manifest `VinTed.addin`.
7. Deploy tất cả vào `%AppData%\Autodesk\ApplicationPlugins\VinTed`.
