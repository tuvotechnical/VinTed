using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Inventor;

namespace VinTed.ExportAutoCAD
{
    /// <summary>
    /// Engine xuất DWG — Port 100% logic từ HNB frmDWGExport + ExportToDWG.
    /// Hỗ trợ: AllSheets, CurrentSheet, Custom, FromTo.
    /// Hỗ trợ: Drawing, Part, Assembly document.
    /// </summary>
    public class ExportAutoCadEngine
    {
        private const string DWG_TRANSLATOR_ID = "{C24E3AC2-122E-11D5-8E91-0010B541CD80}";

        private readonly Inventor.Application _invApp;
        private TranslatorAddIn _dwgAddIn;
        private TranslationContext _context;
        private NameValueMap _options;
        private DataMedium _dataMedium;
        private Document _document;
        private bool _dwgAddInLoaded;

        private int _exportedCount;
        private volatile bool _stopRequested;

        public event EventHandler<ExportAutoCadProgressEventArgs> ProgressChanged;

        public int ExportedCount
        {
            get { return _exportedCount; }
        }

        public bool WasStopped
        {
            get { return _stopRequested; }
        }

        public ExportAutoCadEngine(Inventor.Application invApp)
        {
            _invApp = invApp;
            _exportedCount = 0;
            _stopRequested = false;
        }

        public void RequestStop()
        {
            _stopRequested = true;
        }

        // ====================================================================
        // KHỞI TẠO TRANSLATOR
        // ====================================================================

        /// <summary>
        /// Khởi tạo DWG Translator AddIn. 
        /// Phải gọi trước khi Execute.
        /// </summary>
        public bool InitTranslator()
        {
            try
            {
                _dwgAddIn = _invApp.ApplicationAddIns.get_ItemById(DWG_TRANSLATOR_ID) as TranslatorAddIn;
            }
            catch (Exception)
            {
                _dwgAddInLoaded = false;
                return false;
            }

            if (_dwgAddIn == null)
            {
                _dwgAddInLoaded = false;
                return false;
            }

            if (!_dwgAddIn.Activated)
            {
                _dwgAddIn.Activate();
            }

            _options = _invApp.TransientObjects.CreateNameValueMap();
            _context = _invApp.TransientObjects.CreateTranslationContext();
            _context.Type = IOMechanismEnum.kFileBrowseIOMechanism;
            _dataMedium = _invApp.TransientObjects.CreateDataMedium();
            _document = _invApp.ActiveDocument;
            _dwgAddInLoaded = true;
            return true;
        }

        // ====================================================================
        // KIỂM TRA RASTER VIEW (từ frmDWGExport.CheckDrawingViewIsRasterView)
        // ====================================================================

        /// <summary>
        /// Kiểm tra xem DrawingDocument có view nào đang ở chế độ Raster không.
        /// Trả về true nếu KHÔNG có raster view (an toàn để export).
        /// Trả về false nếu CÓ raster view + gán thông tin vào rasterInfo.
        /// </summary>
        public bool CheckRasterViews(DrawingDocument idwDoc, out string rasterInfo)
        {
            rasterInfo = String.Empty;
            try
            {
                for (int s = 1; s <= idwDoc.Sheets.Count; s++)
                {
                    Sheet sheet = idwDoc.Sheets[s];
                    if (sheet.DrawingViews == null)
                    {
                        continue;
                    }
                    for (int v = 1; v <= sheet.DrawingViews.Count; v++)
                    {
                        DrawingView view = sheet.DrawingViews[v];
                        if (view.IsRasterView)
                        {
                            rasterInfo = String.Format(
                                "Định dạng trên chế độ xem \"{0}\" (Sheet: {1})\nkhông được hỗ trợ khi đang ở trạng thái \"Raster View\".\nVui lòng chuyển tất cả chế độ xem raster thành precise trước khi xuất.",
                                view.Label != null ? view.Label.Text : "?",
                                sheet.Name);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                rasterInfo = "Lỗi kiểm tra Raster View: " + ex.Message;
                return false;
            }
            return true;
        }

        // ====================================================================
        // TẠO FILE INI (từ frmDWGExport.WriteToExportToDWGIniFile + ExportToDWG.LoadConfigurationFile)
        // ====================================================================

        /// <summary>
        /// Tạo file ExportToDWG.ini cho DWG Translator.
        /// </summary>
        public string WriteIniFile(string outputFolder, string exportSpace, bool allSheets)
        {
            string addinFolder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (String.IsNullOrEmpty(addinFolder))
            {
                return String.Empty;
            }

            string version = _invApp.SoftwareVersion.DisplayVersion;
            if (!String.IsNullOrEmpty(version) && version.Length >= 4)
            {
                version = version.Substring(0, 4);
            }
            else
            {
                version = "2023";
            }

            string allSheetsValue = allSheets ? "Yes" : "No";

            string scaling;
            if (String.Equals(exportSpace, "Layout", StringComparison.OrdinalIgnoreCase))
            {
                scaling = "Text";
            }
            else
            {
                scaling = "Geometry";
            }

            string iniPath = System.IO.Path.Combine(addinFolder, "ExportToDWG.ini");

            string contents = "\r\n[EXPORT SELECT OPTIONS]\r\n" +
                "AUTOCAD VERSION=AutoCAD 2007\r\n" +
                "CREATE AUTOCAD MECHANICAL=No\r\n" +
                "USE TRANSMITTAL=No\r\n" +
                "USE CUSTOMIZE=No\r\n" +
                "CUSTOMIZE FILE=C:\\Users\\Public\\Documents\\Autodesk\\Inventor " + version +
                "\\Design Data\\DWG-DXF\\FlatPattern.xml\r\n" +
                "CREATE LAYER GROUP=No\r\n" +
                "PARTS ONLY=No\r\n" +
                "REPLACE SPLINE=No\r\n" +
                "CHORD TOLERANCE=0.001000\r\n" +
                "[EXPORT PROPERTIES]\r\n" +
                "SELECTED PROPERTIES=\r\n" +
                "[EXPORT DESTINATION]\r\n" +
                "SPACE=" + exportSpace + "\r\n" +
                "SCALING=" + scaling + "\r\n" +
                "ALL SHEETS=" + allSheetsValue + "\r\n" +
                "MAPPING=MapsBest\r\n" +
                "MODEL GEOMETRY ONLY=No\r\n" +
                "EXPLODE DIMENSIONS=No\r\n" +
                "SYMBOLS ARE BLOCKED=Yes\r\n" +
                "AUTOCAD TEMPLATE=\r\n" +
                "DESTINATION DXF=No\r\n" +
                "USE ACI FOR ENTITIES AND LAYERS=Yes\r\n" +
                "ALLOW RASTER VIEWS=No\r\n" +
                "SHOW DESTINATION PAGE=No\r\n" +
                "ENABLE POSTPROCESS=No\r\n" +
                "[EXPORT LINE TYPE & LINE SCALE]\r\n" +
                "LINE TYPE FILE=C:\\Users\\Public\\Documents\\Autodesk\\Inventor " + version +
                "\\COMPATIBILITY\\Support\\invISO.lin\r\n" +
                "Continuous=Continuous;0.\r\n" +
                "Dashed=DASHED;0.\r\n" +
                "Dashed Space=DASHED_SPACE;0.\r\n" +
                "Long Dash Dotted=LONG_DASH_DOTTED;0.\r\n" +
                "Long Dash Double Dot=LONG_DASH_DOUBLE_DOT;0.\r\n" +
                "Long Dash Triple Dot=LONG_DASH_TRIPLE_DOT;0.\r\n" +
                "Dotted=DOTTED;0.\r\n" +
                "Chain=CHAIN;0.\r\n" +
                "Double Dash Chain=DOUBLE_DASH_CHAIN;0.\r\n" +
                "Dash Double Dot=DASH_DOUBLE_DOT;0.\r\n" +
                "Dash Dot=DASH_DOT;0.\r\n" +
                "Double Dash Dot=DOUBLE_DASH_DOT;0.\r\n" +
                "Double Dash Double Dot=DOUBLE_DASH_DOUBLE_DOT;0.\r\n" +
                "Dash Triple Dot=DASH_TRIPLE_DOT;0.\r\n" +
                "Double Dash Triple Dot=DOUBLE_DASH_TRIPLE_DOT;0.\r\n";

            System.IO.File.WriteAllText(iniPath, contents);
            return iniPath;
        }

        // ====================================================================
        // ĐỔI TÊN SHEET (từ frmDWGExport.RenameSheet)
        // ====================================================================

        /// <summary>
        /// Đổi tên tất cả Sheet thành 1, 2, 3... cho output gọn.
        /// </summary>
        public bool RenameSheets(Sheets sheets)
        {
            int num = 0;
            try
            {
                for (int i = 1; i <= sheets.Count; i++)
                {
                    num++;
                    sheets[i].Name = num.ToString();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ====================================================================
        // XUẤT DRAWING — Logic chính (từ frmDWGExport.PublishDWG)
        // ====================================================================

        /// <summary>
        /// Thực thi xuất DWG cho DrawingDocument.
        /// Logic 100% từ frmDWGExport.PublishDWG.
        /// </summary>
        public void ExecuteDrawing(DrawingDocument idwDoc, ExportAutoCadOptions opts, string outputFilePath)
        {
            if (!_dwgAddInLoaded)
            {
                throw new InvalidOperationException("DWG Translator chưa được khởi tạo.");
            }

            _exportedCount = 0;
            _stopRequested = false;

            // --- Cấu hình options (từ PublishDWG) ---
            if (_dwgAddIn.get_HasSaveCopyAsOptions(_invApp.ActiveDocument, _context, _options))
            {
                string iniPath = WriteIniFile(
                    opts.OutputFolder,
                    opts.ExportSpace,
                    false); // Báo cáo: "Cấm Inventor xuất toàn bộ các trang cùng lúc" (All Sheets = No)

                if (!System.IO.File.Exists(iniPath))
                {
                    throw new InvalidOperationException("Không thể truy cập tệp cấu hình \"ExportToDWG.ini\"");
                }

                _options.set_Value("Export_Acad_IniFile", iniPath);
            }

            // --- Transaction wrapper (từ frmDWGExport.PublishDWG) ---
            Transaction transaction = null;
            bool originalScreenUpdating = true;
            bool originalSilentOperation = false;
            bool originalBackgroundUpdates = false;
            bool originalDeferUpdates = false;
            ApplicationAddIn vaultAddIn = null;

            try
            {
                try
                {
                    originalScreenUpdating = _invApp.ScreenUpdating;
                    originalSilentOperation = _invApp.SilentOperation;
                    originalBackgroundUpdates = _invApp.DrawingOptions.EnableBackgroundUpdates;
                    originalDeferUpdates = idwDoc.DrawingSettings.DeferUpdates;

                    if (opts.DisableScreenUpdating) _invApp.ScreenUpdating = false;
                    if (opts.EnableSilentOperation) _invApp.SilentOperation = true;
                    
                    // Tối ưu hóa theo báo cáo: Tắt Background Updates và bật DeferUpdates
                    _invApp.DrawingOptions.EnableBackgroundUpdates = false;
                    idwDoc.DrawingSettings.DeferUpdates = true;

                    // Tắt Vault Addin để tránh vi độ trễ check-in/out
                    vaultAddIn = _invApp.ApplicationAddIns.get_ItemById("{48B682BC-42E6-4953-84C5-3D253B52E77B}");
                    if (vaultAddIn != null && vaultAddIn.Activated) vaultAddIn.Deactivate();
                }
                catch (Exception) { }

                transaction = _invApp.TransactionManager.StartTransaction(
                    (_Document)_document, "VinTed Export DWG");

                // Kiểm tra raster view
                string rasterInfo;
                if (!CheckRasterViews(idwDoc, out rasterInfo))
                {
                    Report(0, 0, String.Empty, String.Empty, rasterInfo);
                    return;
                }

                int totalSheets = idwDoc.Sheets.Count;

                switch (opts.SheetMode)
                {
                    case SheetExportMode.AllSheets:
                        ExecuteAllSheets(idwDoc, opts, outputFilePath, totalSheets);
                        break;

                    case SheetExportMode.CurrentSheet:
                        ExecuteCurrentSheet(idwDoc, opts);
                        break;

                    case SheetExportMode.Custom:
                        ExecuteCustomSheets(idwDoc, opts, outputFilePath, totalSheets);
                        break;

                    case SheetExportMode.FromTo:
                        ExecuteFromToSheets(idwDoc, opts, outputFilePath);
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                try
                {
                    _invApp.ScreenUpdating = originalScreenUpdating;
                    _invApp.SilentOperation = originalSilentOperation;
                    _invApp.DrawingOptions.EnableBackgroundUpdates = originalBackgroundUpdates;
                    idwDoc.DrawingSettings.DeferUpdates = originalDeferUpdates;

                    if (vaultAddIn != null && !vaultAddIn.Activated) vaultAddIn.Activate();
                }
                catch (Exception) { }

                if (transaction != null)
                {
                    try
                    {
                        transaction.End();
                    }
                    catch (Exception) { }
                    try
                    {
                        Marshal.ReleaseComObject(transaction);
                    }
                    catch (Exception) { }
                    transaction = null;
                }
            }
        }

        // --- AllSheets mode (từ frmDWGExport, branch rbAllSheets.Checked) ---
        private void ExecuteAllSheets(DrawingDocument idwDoc, ExportAutoCadOptions opts,
            string outputFilePath, int totalSheets)
        {
            Report(0, totalSheets, String.Empty, String.Empty, "Đang đổi tên sheet...");
            PumpMessages();

            if (!RenameSheets(idwDoc.Sheets))
            {
                throw new InvalidOperationException("Không thể đổi tên sheet.");
            }

            _exportedCount = 0;

            for (int i = 1; i <= totalSheets; i++)
            {
                PumpMessages();
                if (_stopRequested)
                {
                    Report(_exportedCount, totalSheets, String.Empty, String.Empty, "Đã dừng theo yêu cầu.");
                    return;
                }

                Sheet sheet = idwDoc.Sheets[i];
                if (sheet == null) { continue; }

                string sheetName = sheet.Name;
                string filePath = System.IO.Path.Combine(opts.OutputFolder,
                    String.Format("{0}_{1}.dwg", opts.BaseFileName, i));

                Report(_exportedCount, totalSheets, sheetName, filePath,
                    String.Format("Đang xuất sheet {0}/{1}: {2}", _exportedCount + 1, totalSheets, sheetName));
                PumpMessages();

                ExportSingleSheet(sheet, filePath);
                _exportedCount++;

                // Báo cáo: "lập tức giải phóng không gian bộ nhớ" sau mỗi sheet để duy trì dung lượng RAM trần ổn định.
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Report(_exportedCount, totalSheets, sheetName, filePath,
                    String.Format("Đã xuất {0}/{1} sheet", _exportedCount, totalSheets));
            }
        }

        // --- CurrentSheet mode (từ frmDWGExport, branch rbCurrrentSheet.Checked) ---
        private void ExecuteCurrentSheet(DrawingDocument idwDoc, ExportAutoCadOptions opts)
        {
            Sheet activeSheet = idwDoc.ActiveSheet;
            string fileName = String.Format("{0}_{1}.dwg", opts.BaseFileName, activeSheet.Name);
            string filePath = System.IO.Path.Combine(opts.OutputFolder, fileName);

            Report(0, 1, activeSheet.Name, filePath, "Đang xuất sheet hiện tại...");
            PumpMessages();

            if (_stopRequested) { return; }

            ExportSingleSheet(activeSheet, filePath);
            _exportedCount = 1;

            Report(1, 1, activeSheet.Name, filePath, "Hoàn tất xuất sheet hiện tại");
        }

        // --- Custom mode (từ frmDWGExport, branch rbCustomed.Checked) ---
        private void ExecuteCustomSheets(DrawingDocument idwDoc, ExportAutoCadOptions opts,
            string outputFilePath, int totalSheets)
        {
            List<int> indices = ParseCustomSheets(opts.CustomSheets, totalSheets);
            if (indices.Count == 0)
            {
                throw new InvalidOperationException("Không có sheet hợp lệ để xuất.");
            }

            int total = indices.Count;
            _exportedCount = 0;

            foreach (int idx in indices)
            {
                PumpMessages();
                if (_stopRequested)
                {
                    Report(_exportedCount, total, String.Empty, String.Empty, "Đã dừng theo yêu cầu.");
                    return;
                }

                Sheet sheet = idwDoc.Sheets[idx];
                if (sheet == null) { continue; }

                string sheetName = sheet.Name;
                string filePath;
                if (sheetName.Contains(":"))
                {
                    filePath = System.IO.Path.Combine(opts.OutputFolder,
                        sheetName.Replace(":", "_") + ".dwg");
                }
                else
                {
                    filePath = System.IO.Path.Combine(opts.OutputFolder,
                        String.Format("{0}_{1}.dwg", sheetName, idx));
                }

                Report(_exportedCount, total, sheetName, filePath,
                    String.Format("Đang xuất sheet {0}/{1}: {2}", _exportedCount + 1, total, sheetName));
                PumpMessages();

                ExportSingleSheet(sheet, filePath);
                _exportedCount++;

                // Báo cáo: giải phóng bộ nhớ
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Report(_exportedCount, total, sheetName, filePath,
                    String.Format("Đã xuất {0}/{1} sheet", _exportedCount, total));
            }
        }

        // --- FromTo mode (từ frmDWGExport, branch rbFrom.Checked) ---
        private void ExecuteFromToSheets(DrawingDocument idwDoc, ExportAutoCadOptions opts,
            string outputFilePath)
        {
            int from = opts.FromSheet;
            int to = opts.ToSheet;
            int total = to - from + 1;
            _exportedCount = 0;

            for (int j = from; j <= to; j++)
            {
                PumpMessages();
                if (_stopRequested)
                {
                    Report(_exportedCount, total, String.Empty, String.Empty, "Đã dừng theo yêu cầu.");
                    return;
                }

                Sheet sheet = idwDoc.Sheets[j];
                if (sheet == null) { continue; }

                string sheetName = sheet.Name;
                string filePath = System.IO.Path.Combine(opts.OutputFolder,
                    String.Format("{0}_{1}.dwg", opts.BaseFileName, j));

                Report(_exportedCount, total, sheetName, filePath,
                    String.Format("Đang xuất sheet {0}/{1}: {2}", _exportedCount + 1, total, sheetName));
                PumpMessages();

                ExportSingleSheet(sheet, filePath);
                _exportedCount++;

                // Báo cáo: giải phóng bộ nhớ
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Report(_exportedCount, total, sheetName, filePath,
                    String.Format("Đã xuất {0}/{1} sheet", _exportedCount, total));
            }
        }

        // ====================================================================
        // XUẤT PART / ASSEMBLY (từ frmDWGExport.PublishDWG, branch Part/Assembly)
        // ====================================================================

        /// <summary>
        /// Xuất Part hoặc Assembly sang DWG.
        /// </summary>
        public void ExecutePartAssembly(ExportAutoCadOptions opts)
        {
            if (!_dwgAddInLoaded)
            {
                throw new InvalidOperationException("DWG Translator chưa được khởi tạo.");
            }

            _exportedCount = 0;

            if (_dwgAddIn.get_HasSaveCopyAsOptions(_invApp.ActiveDocument, _context, _options))
            {
                _options.set_Value("Solid", true);
                _options.set_Value("Surface", true);
                _options.set_Value("Sketch", true);
                _options.set_Value("DwgVersion", 27);
            }

            string outputFile = System.IO.Path.Combine(opts.OutputFolder, opts.BaseFileName + ".dwg");

            Report(0, 1, String.Empty, outputFile, "Đang xuất 3D sang DWG...");
            PumpMessages();

            DeleteFileIfExists(outputFile);

            _dataMedium.FileName = outputFile;
            _dwgAddIn.SaveCopyAs(_document, _context, _options, _dataMedium);
            _exportedCount = 1;

            Report(1, 1, String.Empty, outputFile, "Hoàn tất xuất DWG");

            // Mở Explorer tới file (giống iLogic)
            if (System.IO.File.Exists(outputFile))
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe",
                        String.Format("/select,\"{0}\"", outputFile));
                }
                catch (Exception) { }
            }
        }

        // ====================================================================
        // XUẤT MỘT SHEET (từ frmDWGExport.ExportCustomSheet)
        // ====================================================================

        /// <summary>
        /// Xuất một Sheet cụ thể sang DWG.
        /// Logic: Activate sheet → set file name → SaveCopyAs.
        /// </summary>
        private void ExportSingleSheet(Sheet sheet, string filePath)
        {
            DrawingDocument idwDoc = _document as DrawingDocument;
            if (idwDoc != null && idwDoc.ActiveSheet != sheet)
            {
                sheet.Activate();
            }

            DeleteFileIfExists(filePath);

            _dataMedium.FileName = filePath;
            _dwgAddIn.SaveCopyAs(_document, _context, _options, _dataMedium);
        }

        // ====================================================================
        // HIỂN THỊ OPTIONS DIALOG CỦA DWG TRANSLATOR (từ frmDWGExport.btnOptions)
        // ====================================================================

        /// <summary>
        /// Mở dialog DWG Translator Options của Inventor.
        /// </summary>
        public void ShowTranslatorOptions()
        {
            if (_dwgAddIn != null && _dwgAddInLoaded)
            {
                _dwgAddIn.ShowSaveCopyAsOptions(_invApp.ActiveDocument, _context, _options);
            }
        }

        // ====================================================================
        // UTILITIES
        // ====================================================================

        /// <summary>
        /// Parse chuỗi custom sheets "1,3,5" thành danh sách int.
        /// Loại bỏ giá trị ngoài phạm vi và trùng lặp.
        /// (từ frmDWGExport, branch rbCustomed)
        /// </summary>
        public List<int> ParseCustomSheets(string input, int maxSheetCount)
        {
            List<int> result = new List<int>();
            if (String.IsNullOrEmpty(input))
            {
                return result;
            }

            string[] parts = input.Split(new char[] { ',' });
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (String.IsNullOrEmpty(trimmed)) { continue; }

                int num;
                if (!Int32.TryParse(trimmed, out num)) { continue; }
                if (num < 1 || num > maxSheetCount) { continue; }
                if (result.Contains(num)) { continue; }

                result.Add(num);
            }
            return result;
        }

        /// <summary>
        /// Xóa các file DWG cũ trong thư mục (từ frmDWGExport.DeleteDWGfile).
        /// </summary>
        public bool DeleteExistingDwgFiles(string folder)
        {
            try
            {
                string[] files = System.IO.Directory.GetFiles(folder, "*.dwg");
                for (int i = 0; i < files.Length; i++)
                {
                    System.IO.File.Delete(files[i]);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Đếm số file DWG trong thư mục.
        /// </summary>
        public int CountDwgFiles(string folder)
        {
            try
            {
                if (!System.IO.Directory.Exists(folder)) { return 0; }
                return System.IO.Directory.GetFiles(folder, "*.dwg").Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private void DeleteFileIfExists(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception) { }
        }

        private void PumpMessages()
        {
            try
            {
                System.Windows.Forms.Application.DoEvents();
            }
            catch (Exception) { }
        }

        private void Report(int current, int total, string sheetName, string outputFile, string message)
        {
            EventHandler<ExportAutoCadProgressEventArgs> handler = ProgressChanged;
            if (handler != null)
            {
                handler(this, new ExportAutoCadProgressEventArgs(current, total, sheetName, outputFile, message));
            }
        }
        // ====================================================================
        // GỘP FILE DWG BẰNG AUTOCAD CORE CONSOLE
        // ====================================================================

        public void MergeDwgFiles(string outputFolder, string outputFile, double gap, bool deleteFiles)
        {
            string addinFolder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string pluginPath = System.IO.Path.Combine(addinFolder, "VinTed.AutoCAD.dll");
            
            if (!System.IO.File.Exists(pluginPath))
            {
                throw new InvalidOperationException("Không tìm thấy VinTed.AutoCAD.dll để gộp file.");
            }

            string autocadPath = @"C:\Program Files\Autodesk\AutoCAD 2024\accoreconsole.exe";
            if (!System.IO.File.Exists(autocadPath))
            {
                throw new InvalidOperationException("Không tìm thấy AutoCAD Core Console tại: " + autocadPath);
            }

            // Ghi cấu hình ra file args
            string argsFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VinTed_MergeArgs.txt");
            string[] lines = new string[]
            {
                outputFolder,
                outputFile,
                gap.ToString(),
                deleteFiles.ToString()
            };
            System.IO.File.WriteAllLines(argsFile, lines);

            // Tạo file script cho AutoCAD
            string scrFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VinTed_Merge.scr");
            string logFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VinTed_Merge_Log.txt");
            string scrContent = String.Format("SECURELOAD\r\n0\r\nNETLOAD \"{0}\"\r\nVINTED_MERGE\r\n", pluginPath.Replace("\\", "\\\\"));
            System.IO.File.WriteAllText(scrFile, scrContent);

            // Chạy accoreconsole
            Report(0, 1, String.Empty, String.Empty, "Đang gọi AutoCAD gộp file (chạy ngầm)...");
            PumpMessages();

            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
            psi.FileName = autocadPath;
            // Dùng template trống mặc định của acad
            psi.Arguments = String.Format("/s \"{0}\" /l \"{1}\"", scrFile, logFile);
            psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            using (System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi))
            {
                p.WaitForExit(60000); // Đợi tối đa 60 giây
            }

            try { System.IO.File.Delete(argsFile); } catch { }
            try { System.IO.File.Delete(scrFile); } catch { }
        }
    }
}
