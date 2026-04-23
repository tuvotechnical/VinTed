# iLOGIC MIGRATION & LOGIC PRESERVATION

## Nguồn dữ liệu
Các file script gốc (`*.iLogicVb`, `*.cs`) nằm trong thư mục `ilogic/` tại gốc dự án:
- `CopyHatch.iLogicVb` — Logic sao chép mặt cắt (Hatch)
- `ExportAutocad.cs` — Logic xuất và gộp bản vẽ DWG
- `Find And Replace.iLogicVb` — Logic tìm kiếm và thay thế Text
- `frmInsertPlus.cs` — Logic gắn phần cứng tự động

## Quy tắc bảo toàn (CRITICAL)
Khi được yêu cầu bổ sung một tính năng iLogic có sẵn vào Add-in:

1. **ĐỌC FILE GỐC:** PHẢI đọc file `.iLogicVb` / `.cs` tương ứng trong `ilogic/` trước khi viết code.
2. **BẢO TOÀN TUYỆT ĐỐI:** Trích xuất và giữ nguyên:
   - Luồng xử lý (flow)
   - Thuật toán tính toán
   - Cách tương tác với Inventor API hiện có
3. **KHÔNG TỰ Ý VIẾT LẠI:** Không tìm giải pháp mới hay viết lại logic để tránh phát sinh lỗi hệ thống.
4. **THAY THẾ UI:** Toàn bộ giao diện cũ (nếu có trong script iLogic) phải được loại bỏ và thiết kế lại hoàn toàn bằng WPF.
5. **KẾT NỐI:** Logic điều khiển từ giao diện WPF mới phải gọi chuẩn xác vào luồng xử lý cốt lõi đã được trích xuất.
