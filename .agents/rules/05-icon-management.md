---
trigger: always_on
---

# ICON MANAGEMENT — ICONIFY API

## Quy tắc tuyệt đối
- **KHÔNG** yêu cầu người dùng cung cấp, tải xuống hoặc chèn file ảnh `.ico`, `.png` cho các nút bấm (Ribbon Button).
- Tự động xử lý toàn bộ Icon bằng **Iconify API**.

## Iconify API
Iconify là framework mã nguồn mở tổng hợp hơn 150+ bộ icon phổ biến (Material Design, FontAwesome, Bootstrap Icons, Tabler...).

### Ưu điểm
- Hoàn toàn miễn phí (Open-source)
- Không cần tạo tài khoản hay API Key
- Trả về định dạng SVG siêu nhẹ, raw data hoặc HTML string
- Tốc độ phản hồi cực nhanh, có thể self-host

### QUY TẮC MÀU SẮC ICON (BẮT BUỘC)
- Luôn sử dụng icon có màu sắc (full color hoặc themed color)
- Không dùng icon đơn sắc mặc định (đen/trắng), trừ: trạng thái disabled, UI tối giản có chủ đích
- Icon phải: dễ nhận diện trong < 1 giây, có độ tương phản tốt, đồng bộ với theme hệ thống

### REST Endpoints
```
# Tìm kiếm icon
GET https://api.iconify.design/search?query=shopping-cart&limit=10

# Lấy file SVG trực tiếp
GET https://api.iconify.design/mdi-light/cart.svg
```

### Cách tích hợp vào dự án
1. Tải mã SVG bằng C# `WebRequest` / `HttpClient`
2. Parse SVG Path Data để nhúng vào XAML (cho giao diện WPF)
3. Hoặc render sang Bitmap in-memory → convert thành `IPictureDisp` (cho Inventor Ribbon Button)
