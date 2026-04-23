---
trigger: always_on
---

# WPF UI/UX DESIGN RULES

## Nguyên tắc chung
- Sử dụng WPF cho mọi giao diện. Tuyệt đối KHÔNG dùng WinForms cho UI.
- Áp dụng mô hình MVVM cơ bản hoặc Code-behind tối giản — ưu tiên tính thực dụng, không over-engineering.
- Sử dụng thư viện **ModernWpf** với **Light Theme** (Windows 10/11 Design) làm nền tảng UI.

## Window Behavior
- Dialog WPF phải set: `WindowStartupLocation = WindowStartupLocation.CenterScreen`
- Set `Topmost = true` hoặc set Owner là handle của cửa sổ Inventor để UI không bị chìm ra phía sau.
- Có thể nhúng vào Custom Dockable Window của Inventor thông qua WinForms `ElementHost` nếu cần.

## Aesthetics (BẮT BUỘC)
Mọi giao diện WPF tạo ra **PHẢI** hiện đại, sang trọng và gây ấn tượng (WOW) từ cái nhìn đầu tiên. Không thiết kế UI sơ sài kiểu WinForms thập niên cũ.

### Color Palette & Theme
- Sử dụng **Light Theme** (`ui:ThemeManager.RequestedTheme="Light"`).
- Tránh các màu cơ bản gắt (plain red, blue, green).
- Phối màu có hệ thống:
  - **Nền:** Sử dụng `SystemChromeLowColor` / `#F5F6F7` (sáng, thoáng)
  - **Primary Accent:** `#005DA6`
- Sử dụng bảng màu chuyên nghiệp, tránh màu lòe loẹt.

### Typography & Layout
- Font hiện đại: **Segoe UI**, Roboto
- Phân cấp Text rõ ràng: Header to + in đậm, nội dung nhỏ + nhạt hơn
- Bố cục thoáng đãng với Padding/Margin hợp lý

### Dynamic & Interactive
- Hiệu ứng chuyển màu khi Hover, Click (Micro-animations)
- Bo góc nhẹ `CornerRadius` cho Border, Button, TextBox
- Giao diện phải "sống động", khuyến khích tương tác

### Consistency (Đồng nhất)
Gom tất cả Styles và Colors thành `ResourceDictionary` đặt trong `<Window.Resources>` hoặc `<UserControl.Resources>` để mọi tính năng trong Add-in luôn đồng bộ.
