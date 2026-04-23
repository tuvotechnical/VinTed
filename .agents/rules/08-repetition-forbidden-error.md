---
trigger: always_on
---

# DANH SÁCH LỖI ĐÃ GẶP — KHÔNG ĐƯỢC LẶP LẠI

> **Mục đích:** Ghi nhận tất cả lỗi build/runtime đã từng xảy ra trong quá trình phát triển VinTed.
> Trước khi viết code mới hoặc sửa code, **BẮT BUỘC** phải đọc file này để tránh lặp lại.

---

## ERR-01: Thiếu reference `stdole` và `System.Windows.Forms`
- **Lỗi:** `error CS0246: The type or namespace name 'stdole' could not be found`
- **Nguyên nhân:** `.csproj` không include `stdole.dll` (cần cho `IPictureDisp`) và `System.Windows.Forms` (cần cho `AxHost` icon conversion).
- **Fix:** Thêm vào `.csproj`:
  ```xml
  <Reference Include="System.Windows.Forms" />
  <Reference Include="stdole">
    <HintPath>C:\Program Files\Autodesk\Inventor 2023\Bin\stdole.dll</HintPath>
  </Reference>
  ```

## ERR-02: Xung đột `Application` — CS0104
- **Lỗi:** `error CS0104: 'Application' is an ambiguous reference between 'System.Windows.Application' and 'Inventor.Application'`
- **Nguyên nhân:** File dùng cả `using System.Windows` và `using Inventor` — cả hai namespace đều có class `Application`.
- **Fix:** KHÔNG dùng `using Inventor;` trong file WPF code-behind. Thay bằng fully-qualified: `Inventor.Application`, `Inventor.DrawingDocument`.

## ERR-03: Xung đột `Path` / `File` — CS0104
- **Lỗi:** `error CS0104: 'Path' is an ambiguous reference between 'System.IO.Path' and 'Inventor.Path'`
- **Nguyên nhân:** `using System.IO` + `using Inventor` — cả hai đều có class `Path` và `File`.
- **Fix:** Dùng fully-qualified: `System.IO.Path.Combine(...)`, `System.IO.File.Exists(...)`. KHÔNG dùng `using System.IO;` trong file có `using Inventor;`.

## ERR-04: `CommandBarControlSizeEnum` không tồn tại — CS0234
- **Lỗi:** `error CS0234: The type or namespace name 'CommandBarControlSizeEnum' does not exist in the namespace 'Inventor'`
- **Nguyên nhân:** Enum này không tồn tại trong Inventor 2023 Interop.
- **Fix:** Dùng overload đơn giản `panel.CommandControls.AddButton(btnDef)` thay vì truyền size enum.

## ERR-05: `ControlDefinitions.Item()` không tồn tại — CS1061
- **Lỗi:** `error CS1061: 'Inventor.ControlDefinitions' does not contain a definition for 'Item'`
- **Nguyên nhân:** Inventor COM interop dùng **indexer** chứ không dùng method `.Item()`.
- **Fix:** Dùng `ControlDefinitions["AppZoomSelectCmd"]` thay vì `ControlDefinitions.Item("AppZoomSelectCmd")`.

## ERR-06: ModernWpf.dll không tìm thấy lúc runtime
- **Lỗi:** `Could not load file or assembly 'ModernWpf, PublicKeyToken=null' or one of its dependencies. The system cannot find the file specified.`
- **Nguyên nhân:** Inventor host process (CLR) **KHÔNG** tự probe thư mục add-in để tìm DLL dependencies. Dù `ModernWpf.dll` nằm cạnh `VinTed.dll`, CLR vẫn không thấy.
- **Fix:** Thêm `AppDomain.AssemblyResolve` handler ngay đầu `Activate()`:
  ```csharp
  _addinFolder = System.IO.Path.GetDirectoryName(
      Assembly.GetExecutingAssembly().Location);
  AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
  ```
  Handler:
  ```csharp
  private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
  {
      string name = new AssemblyName(args.Name).Name;
      string path = System.IO.Path.Combine(_addinFolder, name + ".dll");
      if (System.IO.File.Exists(path))
          return Assembly.LoadFrom(path);
      return null;
  }
  ```

## ERR-07: ThemeResource trả về Color thay vì Brush — XAML runtime
- **Lỗi:** `'#FFF2F2F2' is not a valid value for property 'Background'.`
- **Nguyên nhân:** `{ui:ThemeResource SystemChromeLowColor}` trả về kiểu `Color`, nhưng `Background` cần kiểu `Brush`.
- **Fix:** Dùng trực tiếp `Background="#F2F2F2"` hoặc dùng `SolidColorBrush` resource thay vì ThemeResource Color.

## ERR-08: StaticResource không resolve được giữa sibling MergedDictionaries — XAML runtime
- **Lỗi:** `'{DependencyProperty.UnsetValue}' is not a valid value for property 'Foreground'.`
- **Nguyên nhân:** `ControlStyles.xaml` dùng `{StaticResource VtFgSecondary}` tham chiếu key từ `ThemeColors.xaml`. Hai file này là **sibling** dictionaries trong `MergedDictionaries` ở Window — `StaticResource` chỉ look up **lên trên**, không tìm ngang sibling.
- **Fix:** Trong `ControlStyles.xaml`, merge `ThemeColors.xaml` bên trong:
  ```xml
  <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="/VinTed;component/Themes/ThemeColors.xaml"/>
  </ResourceDictionary.MergedDictionaries>
  ```
- **Quy tắc:** Nếu Dictionary B dùng `StaticResource` từ Dictionary A, thì B **phải merge A bên trong** — không được dựa vào thứ tự merge ở Window level.

---

## QUY TẮC CHUNG RÚT RA

### Namespace Inventor — Luôn cảnh giác xung đột
Khi file có `using Inventor;`, các tên sau bị xung đột:
| Tên | `System` namespace | `Inventor` namespace |
|-----|-------------------|---------------------|
| `Application` | `System.Windows.Application` | `Inventor.Application` |
| `Path` | `System.IO.Path` | `Inventor.Path` |
| `File` | `System.IO.File` | `Inventor.File` |

→ **Giải pháp:** Trong file WPF (`*.xaml.cs`): KHÔNG dùng `using Inventor;`, dùng fully-qualified.
→ Trong file engine/logic: dùng `using Inventor;`, dùng fully-qualified cho `System.IO.*`.

### Third-party DLL trong Inventor Add-in
Bất kỳ DLL nào ngoài .NET Framework (ModernWpf, NuGet packages...) đều **BẮT BUỘC** phải có `AppDomain.AssemblyResolve` handler.

### XAML ThemeResource
KHÔNG dùng `ThemeResource *Color` cho property cần `Brush`. Dùng `*Brush` hoặc inline color.
