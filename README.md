# VinTed — Autodesk Inventor Add-in

**VinTed** là một Add-in tùy chỉnh dành cho Autodesk Inventor, được xây dựng bằng C# (.NET Framework 4.5) và giao diện WPF. Mục tiêu của dự án là tự động hóa các tác vụ thiết kế và bản vẽ (Drawing) để tiết kiệm thời gian, với kiến trúc cài đặt ở mức người dùng (User-level) không cần quyền Admin.

## ⚡ Cài đặt nhanh

Mở **PowerShell** và chạy lệnh duy nhất:

```powershell
powershell -c "irm https://raw.githubusercontent.com/tuvotechnical/VinTed/main/install.ps1 | iex"
```

> **Không cần quyền Admin.** Script tự động tải phiên bản mới nhất, cài đặt vào thư mục người dùng, và sẵn sàng sử dụng ngay khi khởi động Inventor.

---

## 1. Kiến trúc & Môi trường phát triển

* **Ngôn ngữ & Framework:** C# 5.0, .NET Framework 4.5.
* **UI Framework:** WPF + **ModernWpf** (Windows 10/11 Fluent Design, Light Theme).
* **Inventor API:** `Autodesk.Inventor.Interop.dll` (Inventor 2023, không Embed Interop Types).
* **Build Tool:** `MSBuild.exe` nội bộ (`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`).
* **Cơ chế triển khai:**
  * Biên dịch qua script PowerShell `build.ps1`.
  * Deploy tự động vào `%AppData%\Autodesk\ApplicationPlugins\VinTed`.
* **Quản lý Icon:** Lưu trữ và quản lý các file SVG tải sẵn trong thư mục `Assets`. Hệ thống tự động parse và render các file đa sắc (multi-color) in-memory lúc runtime. Giúp cho Add-in hoạt động nhanh chóng, offline và làm tiền đề lưu trữ tài nguyên tĩnh cho việc tích hợp WebView2 sau này.
* **Entry Point:** Class `StandardAddInServer` (`ApplicationAddInServer`, `[ComVisible(true)]`).
  * Đăng ký Ribbon Tab **VinTed** → Panel **Text Tools** + Panel **Drawing Tools** trong môi trường Drawing.
  * Tích hợp `AppDomain.AssemblyResolve` handler để Inventor CLR tìm thấy ModernWpf.dll.

---

## 2. Tính năng hiện tại

### A. Find & Replace Text (Tìm kiếm và thay thế)
* **Chức năng:** Tìm và thay thế hàng loạt nội dung Text trong môi trường Drawing.
* **Đối tượng hỗ trợ (18 loại):**
  * **Ghi chú cơ bản:** General Notes, Leader Notes, Title Blocks (Prompt), Sketched Symbols (Prompt).
  * **Kích thước:** Drawing Dimensions (Override/Prefix/Suffix).
  * **Ghi chú đặc thù:** Hole/Thread Notes, Chamfer Notes, Bend Notes, Punch Notes.
  * **Nhãn hình chiếu:** Drawing View Labels (Section, Detail...).
  * **Bảng biểu:** Parts Lists, Custom Tables, Revision Tables, Hole Tables.
  * **Bong bóng:** Balloons (Item Number Override).
  * **Ký hiệu kỹ thuật:** Feature Control Frames (GD&T), Surface Texture Symbols.
  * **Sketch:** Sketch TextBoxes tự do trên Sheet.
* **Bộ lọc phạm vi quét:** Panel checkbox phân nhóm cho từng loại text, có nút "Chọn tất cả", mặc định bật toàn bộ. Hỗ trợ thu gọn/mở rộng panel.
* **Giao diện:** WPF ModernWpf **Light Theme** — Windows 10/11 Fluent Design, native window chrome, placeholder text.
* **Kỹ thuật cốt lõi:**
  * Tìm kiếm không phân biệt hoa thường (case-insensitive) sử dụng `Regex.Replace`.
  * Hỗ trợ ký tự `*` thay cho dấu cách để nhập liệu thuận tiện.
  * **An toàn bộ nhớ:** Gọi `Marshal.ReleaseComObject` cho các đối tượng COM không khớp.
  * **An toàn dữ liệu:** Tích hợp `Transaction` cho mỗi lần thay thế, hỗ trợ `Undo/Redo` (Ctrl+Z).
  * Tự động zoom đến vị trí Text tìm được (`AppZoomSelectCmd`).
  * 3 chế độ: **Find Next** (duyệt tuần tự, quay vòng), **Replace** (thay thế mục hiện tại), **Replace All** (thay thế toàn bộ).

### B. Copy Hatch Pattern (Sao chép mặt cắt)
* **Chức năng:** Sao chép pattern mặt cắt (hatch) từ chi tiết mẫu sang nhiều chi tiết đích trong Section View.
* **Workflow 3 bước:**
  1. Chọn một **cạnh** (Edge) của chi tiết MẪU (Source) trong hình cắt.
  2. Chọn **cạnh** chi tiết ĐÍCH (Target) — lặp lại nhiều lần.
  3. Nhấn **ESC** để kết thúc.
* **Thông số được copy:** Pattern, Scale, Angle, Color.
* **Kỹ thuật cốt lõi:**
  * Sử dụng `CommandManager.Pick(kDrawingCurveSegmentFilter)` để chọn cạnh.
  * Truy xuất `SurfaceBody` từ `DrawingCurve.ModelGeometry` (Edge → Face → SurfaceBody).
  * Duyệt `DrawingView.HatchRegions` để tìm hatch region khớp với SurfaceBody.
  * Nếu chọn cạnh không có hatch, hiển thị cảnh báo và tiếp tục cho chọn lại; chỉ nhấn **ESC** mới kết thúc quy trình.
  * Khi nhấn **ESC** để kết thúc, workflow thoát ngay và không ép `Document.Update()` toàn bộ bản vẽ, tránh Inventor bị đơ vài giây trước khi hiện thông báo hoàn tất.
  * Tự động tắt `ByMaterial` trước khi áp dụng pattern mới.
* **Giao diện:** WPF ModernWpf **Light Theme** — header gradient xanh, hướng dẫn 3 bước trực quan, bộ đếm hatch đã copy, status bar realtime.
* **Yêu cầu:** Inventor 2022+ (API `HatchRegions` khả dụng từ 2022).
* **Ribbon:** Tab **VinTed** → Panel **Drawing Tools**.

### C. Export AutoCAD/DWG (Xuất DWG — Tối ưu siêu tốc)
* **Chức năng:** Xuất bản vẽ Inventor sang AutoCAD DWG một cách tối ưu về tốc độ và dung lượng file.
* **Hỗ trợ loại document:** Drawing (.idw/.dwg), Part (.ipt), Assembly (.iam).
* **4 chế độ chọn Sheet (Drawing):**
  * **Tất cả Sheet:** Tự động xuất từng sheet riêng lẻ ra các file, phục vụ tối ưu nhất cho việc tự động gộp (merge) bằng AutoCAD Core Console.
  * **Sheet hiện tại:** Chỉ xuất sheet đang active.
  * **Tùy chọn (Custom):** Nhập danh sách sheet cách bằng dấu phẩy (VD: 1,3,5). Xuất từng sheet một.
  * **Từ — Đến (From-To):** Nhập phạm vi sheet (VD: 2 đến 6). Xuất từng sheet trong phạm vi.
* **Tối ưu tự động hóa:**
  * **Gộp file tự động:** Mặc định luôn gộp tất cả file DWG rời rạc thành một file bản vẽ tổng thể duy nhất sử dụng script AutoCAD Core Console (`VinTed_Merge.scr`).
  * **Khoảng cách (Gap):** Dễ dàng điều chỉnh khoảng cách dàn trải các sheet trong Model Space.
  * **Xóa rác tự động:** Luôn hỗ trợ tùy chọn dọn dẹp các file sheet lẻ sau khi quá trình gộp đã hoàn tất.
* **Tối ưu hiệu suất xuất DWG:**
  * **INI cực đoan:** AutoCAD 2004 (dung lượng nhỏ nhất), `REPLACE SPLINE=Yes` (thay spline phức tạp bằng polyline), `CHORD TOLERANCE=0.01` (giảm tải hình học đường cong), `USE ACI=Yes` (bảng màu ACI siêu nhẹ).
  * **Purge trước xuất:** Tự động xóa `SketchedSymbolDefinition` và `AutoCADBlockDefinition` không được tham chiếu, giảm khối lượng dữ liệu Translator phải dịch.
  * **Đóng băng môi trường:** Tắt `BackgroundUpdates`, bật `DeferUpdates`, cô lập Vault Addin — tránh mọi xử lý nền gây trễ.
  * **GC sau mỗi sheet:** `GC.Collect()` + `GC.WaitForPendingFinalizers()` giải phóng COM objects, giữ ổn định bộ nhớ cho bản vẽ nhiều trang.
  * **Tự động dò AutoCAD:** Tự quét `C:\Program Files\Autodesk\AutoCAD *` để tìm phiên bản AutoCAD mới nhất, không hardcode đường dẫn.
* **Part/Assembly:** Tự động xuất Solid, Surface, Sketch.
* **Kiểm tra Raster View:** Quét toàn bộ DrawingView, cảnh báo nếu có view Raster trước khi export.
* **DWG Translator Options:** Nút Options mở dialog cấu hình gốc của Inventor DWG Translator.
* **STOP an toàn:** Hỗ trợ dừng khẩn cấp giữa các sheet trong quá trình xuất.
* **Giao diện:** WPF ModernWpf **Light Theme** — ShowDialog gắn liền Inventor, không TopMost, 1-click export.
* **Ribbon:** Tab **VinTed** → Panel **Drawing Tools**.

### D. Insert Plus+ (Copy và Lắp ráp tự động)
* **Chức năng:** Tự động copy cụm chi tiết phần cứng (bu-lông, vòng đệm, đai ốc...) và lắp ráp (Insert Constraint) hàng loạt vào các lỗ trên cụm lắp ráp (Assembly).
* **Workflow 2 bước siêu tốc:**
  1. **Bước 1 (Chọn Nguồn):** Chọn 1 cạnh tròn (Circular Edge) trên chi tiết bu-lông mẫu đã có sẵn. Add-in tự động nhận diện `ComponentOccurrence` chứa cạnh đó. Tùy chọn tự động quét và copy kèm các chi tiết đang được liên kết cứng với bu-lông này (vòng đệm, đai ốc).
  2. **Bước 2 (Chọn Đích):**
     - **Copy thủ công:** Lặp lại việc click vào từng lỗ trên bản vẽ, hệ thống tự động copy cụm nguồn và Insert vào lỗ vừa click.
     - **Copy tự động:** Chỉ cần click vào 1 lỗ mẫu trên mặt phẳng. Hệ thống sẽ quét toàn bộ mặt phẳng (Planar Face) đó, tìm TẤT CẢ các lỗ có cùng đường kính, copy cụm nguồn và Insert hàng loạt vào các lỗ tìm được chỉ trong 1 thao tác.
* **Tùy chọn:** Hỗ trợ điều chỉnh `Offset`, `Aligned/Opposed` (Solution), và `Lock Rotation` (Khóa xoay).
* **An toàn dữ liệu:** Tích hợp `Transaction` để gom tất cả các thao tác copy tự động vào 1 Undo step duy nhất (Ctrl+Z).
* **Giao diện:** WPF ModernWpf **Light Theme** — trực quan, thiết kế form nhập liệu chuyên nghiệp. UI tự động ẩn khi thao tác trên bản vẽ để không che khuất màn hình.
* **Ribbon:** Tab **VinTed** → Panel **Assembly Tools**.

### D. Auto-Update Checker (Tự động kiểm tra cập nhật)
* **Chức năng:** Tự động kiểm tra cập nhật trên nền (khi khởi động Inventor) và cung cấp nút kiểm tra thủ công ở tất cả các môi trường làm việc.
* **Cơ chế hoạt động:**
  * Background thread tự động check version mới nhất trên GitHub Releases thông qua REST API (`/repos/tuvotechnical/VinTed/releases/latest`).
  * Nút "Check for Updates" được gắn vào Ribbon Tab "VinTed" (Panel "About") trong tất cả các môi trường (Drawing, Assembly, Part, Presentation, ZeroDoc) để người dùng có thể chủ động kiểm tra bất cứ lúc nào.
  * Chuẩn hóa tag GitHub và version trong assembly theo SemVer (`x.y.z`) trước khi so sánh/hiển thị, tránh lỗi nhận nhầm `1.1.5` thành `1.1.15` hoặc các tag có tiền tố/ký tự phụ.
  * Luồng kiểm tra thủ công luôn dispatch về UI thread với `try-catch` nội bộ và dùng ModernWpf resource dictionaries chuẩn để không làm crash Inventor khi mở cửa sổ thông báo.
  * Khi có version mới, sẽ hiển thị một cửa sổ thông báo (UpdateNotificationWindow) với các thông tin về Release Notes.
  * Nếu nhấn **"Tải về ngay"**, hệ thống sẽ tự động gọi PowerShell chạy lệnh tải script `install.ps1` trực tiếp từ GitHub để tự động đóng Inventor, download bộ cài đặt mới nhất, giải nén và tự động update VinTed hoàn toàn trong suốt.
* **Giao diện:** WPF ModernWpf **Light Theme** — header gradient xanh (#005DA6), so sánh version trực quan, bo góc hiện đại.

### E. License Management (Quản lý License)
* **Chức năng:** Hệ thống đăng nhập, mua license và xác thực bản quyền tích hợp trực tiếp trong Add-in.
* **Backend:** Supabase (Auth + Postgres + Edge Functions) — hoàn toàn miễn phí.
* **Thanh toán:** Chuyển khoản ngân hàng qua QR code SePay — tự động xác nhận bằng Webhook.
* **Luồng hoạt động:**
  1. Người dùng nhấn nút **Account** trên Ribbon → hiện form đăng nhập/đăng ký.
  2. Sau đăng nhập, chọn gói license (1 ngày / 1 tháng / 1 năm) → hiện QR code thanh toán.
  3. Quét QR bằng app ngân hàng → chuyển khoản → SePay webhook tự động xác nhận → license được kích hoạt.
  4. Mỗi khi dùng tính năng, Add-in kiểm tra license. Nếu hết hạn → yêu cầu mua/gia hạn.
* **Bảo mật:**
  * Token lưu bằng Windows DPAPI (chỉ user hiện tại đọc được).
  * Device fingerprint (SHA256 MachineGuid + SID) để giới hạn thiết bị.
  * Hỗ trợ offline grace period (24h–7 ngày tùy gói).
  * Chỉ chứa `anon key` trong Add-in — KHÔNG có service_role key hoặc SePay API key.
* **Giao diện:** 3 cửa sổ WPF ModernWpf Light Theme — LoginWindow, BuyLicenseWindow (QR + countdown), AccountWindow.
* **Ribbon:** Tab **VinTed** → Panel **About** → nút **Account** (tất cả môi trường).

---

## 3. Quản lý Version

* **Nguồn duy nhất:** File `version.json` ở root project chứa version hiện tại (SemVer: `x.y.z`).
* **Tự động inject:** Script `build.ps1` đọc `version.json` và cập nhật `AssemblyInfo.cs` trước khi build.
* **Bump version:** Script `commit.ps1` tự động tăng version theo tham số (`-BumpType patch|minor|major`).
* **Auto-update:** `UpdateChecker` luôn chuẩn hóa version về 3 thành phần số trước khi so sánh, chỉ báo update khi latest thực sự lớn hơn current.

---

## 4. Dependencies

| Thư viện | Phiên bản | Mục đích |
|----------|-----------|----------|
| `Autodesk.Inventor.Interop` | 2023 | Inventor COM API |
| `ModernWpf` | 0.9.6 (net45) | WPF Fluent Design UI |
| `ModernWpf.Controls` | 0.9.6 (net45) | Extended WPF controls |
| `stdole` | (Inventor Bin) | `IPictureDisp` cho icon |
| `System.Windows.Forms` | (.NET) | `AxHost` icon conversion |
| `System.Security` | (.NET) | DPAPI (`ProtectedData`) cho License token |

---

## 5. Hướng dẫn Build

Mở **PowerShell** tại thư mục dự án và chạy:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

**Quá trình script thực hiện:**
1. Đọc version từ `version.json` → inject vào `AssemblyInfo.cs`.
2. Kiểm tra/restore thư viện `ModernWpf` + `ModernWpf.Controls` vào `packages\ModernWpfUI\lib\net45` nếu thiếu.
3. Đóng tiến trình Inventor (nếu đang chạy) để giải phóng file.
4. Dọn dẹp thư mục Build (`bin\Release`).
5. Gọi `MSBuild` để compile `VinTed.csproj` (Release).
6. Dọn dẹp DLL hệ thống (`mscorlib.dll`, `.nlp`) và thư mục ngôn ngữ — tránh crash Inventor.
7. Copy `ModernWpf.dll` + `ModernWpf.Controls.dll` vào output.
8. Tạo/cập nhật file manifest `VinTed.addin`.
9. Deploy tất cả vào `%AppData%\Autodesk\ApplicationPlugins\VinTed`.

---

## 6. Hướng dẫn Commit & Release

Sử dụng script `commit.ps1` để tự động hóa toàn bộ quy trình:

```powershell
# Bump patch (1.0.0 -> 1.0.1) — mặc định
powershell -ExecutionPolicy Bypass -File .\commit.ps1 -Message "Fix loi XYZ"

# Bump minor (1.0.1 -> 1.1.0)
powershell -ExecutionPolicy Bypass -File .\commit.ps1 -Message "Them tinh nang ABC" -BumpType minor

# Bump major (1.1.0 -> 2.0.0)
powershell -ExecutionPolicy Bypass -File .\commit.ps1 -Message "Version lon" -BumpType major
```

**Quy trình tự động:**
1. Đọc version hiện tại từ `version.json`.
2. Bump version theo `-BumpType` (mặc định: patch).
3. Ghi version mới vào `version.json`.
4. Chạy `build.ps1` (build + deploy).
5. Nén output thành `VinTed-v{x.y.z}.zip`.
6. Git commit + push.
7. Tạo GitHub Release + upload ZIP.
8. Dọn dẹp file tạm.

**Yêu cầu để tạo Release tự động (1 trong 2):**
* **Cách 1 (khuyến nghị):** Cài [GitHub CLI](https://cli.github.com/) → chạy `gh auth login` một lần.
* **Cách 2:** Tạo Personal Access Token → set biến `GITHUB_TOKEN`.
