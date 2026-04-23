# INVENTOR API INTEGRATION RULES

## Entry Point
- Class `StandardAddInServer` kế thừa từ `ApplicationAddInServer`.
- Đánh dấu `[ComVisible(true)]` và đăng ký COM qua `[ProgId]` & `[Guid]`.
- Cả Project được đặt `ComVisible(false)`, do đó class Main **bắt buộc** phải có `[ComVisible(true)]`.
- Implement chuẩn interface với các hàm `Activate` và `Deactivate`.
- Nút bấm Add-in được đăng ký vào Ribbon Tab **Place Views** của môi trường Drawing.

## Transaction (Undo/Redo)
Mọi thao tác làm thay đổi dữ liệu bản vẽ, part hoặc assembly **phải** được bọc trong `Transaction` của Inventor:
```csharp
Transaction txn = invApp.TransactionManager.StartTransaction(
    invApp.ActiveDocument, "Tên thao tác");
try {
    // ... logic thay đổi dữ liệu ...
    txn.End();
} catch {
    txn.Abort();
    throw;
}
```

## COM Object & Memory Management
- Tuân thủ nghiêm ngặt việc giải phóng `Marshal.ReleaseComObject` khi hoàn tất các vòng lặp quét qua components.
- **KHÔNG** gọi `ReleaseComObject` cho object đã gán vào `List` hoặc biến dùng lại cho UI Event sau đó (sẽ gây lỗi *COM object separated from its underlying RCW*).
- Pattern chuẩn: Quét → nếu không khớp thì Release ngay → nếu khớp thì đưa vào List (không Release).

## Namespace quan trọng
- Dù tên file DLL là `Autodesk.Inventor.Interop.dll`, trong code C# luôn dùng: `using Inventor;`
- **KHÔNG** dùng `using Autodesk.Inventor;` (sẽ gây lỗi compile).

## Xung đột Class Color
Khi vẽ Icon hoặc tô màu WPF, luôn sử dụng fully-qualified name `System.Drawing.Color` để tránh nhầm lẫn với `Inventor.Color`.

## Dynamic Binding cho API mới
Các tính năng mới (như `HatchRegions` của Inventor 2022+) được xử lý bằng `dynamic` binding để đảm bảo không lỗi lúc biên dịch trên các phiên bản Inventor cũ hơn.
