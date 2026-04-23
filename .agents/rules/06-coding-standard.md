---
trigger: always_on
---

# CODING STANDARD

## Code hoàn chỉnh
- Viết code hoàn chỉnh, sẵn sàng để build.
- Tuyệt đối **KHÔNG** để lại các comment kiểu `// Add your logic here` hoặc code cắt xén (placeholder).
- Mọi method, class, logic đều phải hoàn thiện 100%.

## Error Handling (BẮT BUỘC)
Wrap toàn bộ logic thực thi vào khối `try-catch`:
```csharp
try
{
    // Logic thực thi
}
catch (Exception ex)
{
    System.Windows.MessageBox.Show(
        "Lỗi: " + ex.Message,
        "VinTed Error",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
}
```
- Bất kỳ exception nào cũng phải được hiển thị ngắn gọn qua `MessageBox`.
- **KHÔNG BAO GIỜ** được phép làm crash tiến trình gốc của Inventor.

## Documentation Sync (BẮT BUỘC)
Mỗi lần trước khi code thêm tính năng mới hoặc trước khi chạy lệnh build, **phải cập nhật lại thông tin tính năng đó trong file `Readme.md`** để đảm bảo tài liệu luôn đồng bộ với mã nguồn.

## Error Log (BẮT BUỘC)
- **Trước khi viết code**, phải đọc file `.agents/rules/08-repetition-forbidden-error.md` để tránh lặp lại lỗi đã gặp.
- **Khi gặp lỗi build hoặc runtime mới**, phải cập nhật lỗi đó vào `08-repetition-forbidden-error.md` ngay sau khi fix xong, bao gồm:
  - Mã lỗi (ERR-xx)
  - Nội dung lỗi gốc
  - Nguyên nhân gốc rễ
  - Cách fix chuẩn
- Mục tiêu: **KHÔNG BAO GIỜ** mắc lại cùng một lỗi hai lần.