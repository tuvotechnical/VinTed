---
trigger: always_on
---

# BUILD & DEPLOYMENT RULES

## No-Admin & No-Visual Studio
- **KHÔNG** sử dụng Visual Studio IDE.
- **KHÔNG** yêu cầu quyền Administrator.
- Build Tool: `MSBuild.exe` tại `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`

## Deployment Path (User-level)
- Add-in được cài vào: `%AppData%\Autodesk\ApplicationPlugins\VinTed`
- Tuyệt đối **KHÔNG** ghi vào `C:\Program Files`.

## File .csproj
Tạo file `.csproj` tương thích MSBuild 4.0:
- `TargetFrameworkVersion`: v4.5
- `OutputType`: Library (DLL)
- Include: `PresentationCore`, `PresentationFramework`, `WindowsBase`, `System.Xaml` (cho WPF)
- Reference đường dẫn Inventor API: `C:\Program Files\Autodesk\Inventor 20xx\Bin\Public Assemblies\Autodesk.Inventor.Interop.dll`

## File Manifest (.addin)
- `<Assembly>`: Trỏ đến đường dẫn DLL trong AppData
- `<AddinType>`: Standard
- `<LoadOnStartUp>`: 1

## Script build.ps1 (Quy trình)
Mỗi khi được yêu cầu "Build", phải tạo/cập nhật script `build.ps1` thực hiện:

1. **Kill Inventor** — Đóng tiến trình `Inventor.exe` (nếu đang chạy) để giải phóng file DLL cũ.
2. **Clean** — Dọn dẹp thư mục Build (xóa rác).
3. **MSBuild** — Chạy `MSBuild` để compile `VinTed.csproj` với `/t:Rebuild /p:Configuration=Release`.
4. **Remove System DLLs** — Dọn dẹp các DLL hệ thống (`mscorlib.dll`, `.nlp`) khỏi output. Nếu có, CLR host của Inventor sẽ crash ngay lập tức.
5. **Create/Update .addin** — Tự động tạo/cập nhật file `.addin` (XML manifest) với ClientID cố định.
6. **Deploy** — Copy `VinTed.dll` và `/VinTed.addin` vào đúng thư mục `%AppData%`.

## Lệnh build
```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```