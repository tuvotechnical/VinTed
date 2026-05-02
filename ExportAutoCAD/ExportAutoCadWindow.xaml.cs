using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace VinTed.ExportAutoCAD
{
    /// <summary>
    /// Code-behind cho ExportAutoCadWindow.
    /// Port 100% logic UI từ HNB frmDWGExport.cs.
    /// KHÔNG dùng using Inventor; để tránh xung đột namespace (ERR-02, ERR-03).
    /// </summary>
    public partial class ExportAutoCadWindow : Window
    {
        private readonly Inventor.Application _invApp;
        private ExportAutoCadEngine _engine;
        private int _sheetCount;
        private bool _isRunning;
        private bool _isDrawing;

        public ExportAutoCadWindow(Inventor.Application invApp)
        {
            InitializeComponent();
            _invApp = invApp;
            _isRunning = false;
            InitializeDefaults();
        }

        /// <summary>
        /// Constructor cho Drawing document (backward compatible với StandardAddInServer).
        /// </summary>
        public ExportAutoCadWindow(Inventor.Application invApp, Inventor.DrawingDocument drawDoc)
        {
            InitializeComponent();
            _invApp = invApp;
            _isRunning = false;
            InitializeDefaults();
        }

        // ====================================================================
        // KHỞI TẠO MẶC ĐỊNH (từ frmDWGExport_Load)
        // ====================================================================

        private void InitializeDefaults()
        {
            try
            {
                Inventor.DocumentTypeEnum docType = _invApp.ActiveDocumentType;

                if (docType == Inventor.DocumentTypeEnum.kDrawingDocumentObject)
                {
                    _isDrawing = true;
                    Inventor.DrawingDocument idwDoc = (Inventor.DrawingDocument)_invApp.ActiveDocument;
                    _sheetCount = idwDoc.Sheets.Count;

                    // Đặt tên file mặc định — thử lấy PTC code property (giống iLogic)
                    string defaultName = GetPtcCode(idwDoc);
                    if (String.IsNullOrEmpty(defaultName))
                    {
                        defaultName = GetFileNameWithoutExtension(idwDoc.FullFileName);
                    }
                    txtBaseFileName.Text = defaultName;

                    // Sheet range
                    txtFrom.Text = "1";
                    txtTo.Text = _sheetCount.ToString();
                    lblSheetCount.Text = String.Format("/ {0}", _sheetCount);

                    // Export Style mặc định: Model
                    cmbExportSpace.SelectedIndex = 0;
                }
                else if (docType == Inventor.DocumentTypeEnum.kPartDocumentObject ||
                         docType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    _isDrawing = false;
                    _sheetCount = 0;

                    txtBaseFileName.Text = GetFileNameWithoutExtension(_invApp.ActiveDocument.FullFileName);

                    // Disable sheet options cho Part/Assembly (giống iLogic)
                    rbAllSheets.IsEnabled = false;
                    rbCurrentSheet.IsEnabled = false;
                    rbCustom.IsEnabled = false;
                    rbFromTo.IsEnabled = false;
                    txtCustomSheets.IsEnabled = false;
                    txtFrom.IsEnabled = false;
                    txtTo.IsEnabled = false;
                    cmbExportSpace.IsEnabled = false;
                    chkOptimizedIni.IsEnabled = false;
                }

                // Thư mục lưu mặc định: WorkspacePath\CAD (giống iLogic)
                txtOutputFolder.Text = GetDefaultOutputFolder();

                ApplySheetModeUi();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        /// <summary>
        /// Thử đọc property "PTC code" từ PropertySet 4 (Custom).
        /// Giống iLogic: idwDoc.PropertySets[4]["PTC code"].Value
        /// </summary>
        private string GetPtcCode(Inventor.DrawingDocument idwDoc)
        {
            try
            {
                object val = idwDoc.PropertySets[4]["PTC code"].Value;
                if (val != null)
                {
                    string s = val.ToString();
                    if (!String.IsNullOrEmpty(s))
                    {
                        return s;
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        private string GetFileNameWithoutExtension(string fullPath)
        {
            try
            {
                if (!String.IsNullOrEmpty(fullPath))
                {
                    return System.IO.Path.GetFileNameWithoutExtension(fullPath);
                }
            }
            catch (Exception) { }
            return "Drawing";
        }

        private string GetDefaultOutputFolder()
        {
            try
            {
                string workspace = _invApp.DesignProjectManager.ActiveDesignProject.WorkspacePath;
                if (!String.IsNullOrEmpty(workspace))
                {
                    return workspace + "\\CAD";
                }
            }
            catch (Exception) { }

            try
            {
                string file = _invApp.ActiveDocument.FullFileName;
                if (!String.IsNullOrEmpty(file))
                {
                    string folder = System.IO.Path.GetDirectoryName(file);
                    if (!String.IsNullOrEmpty(folder))
                    {
                        return System.IO.Path.Combine(folder, "CAD");
                    }
                }
            }
            catch (Exception) { }

            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        // ====================================================================
        // SHEET MODE UI (từ frmDWGExport: rbAllSheets/rbCurrrentSheet/rbCustomed/rbFrom CheckedChanged)
        // ====================================================================

        private void SheetMode_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplySheetModeUi();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ApplySheetModeUi()
        {
            if (rbAllSheets == null || rbCurrentSheet == null || rbCustom == null || rbFromTo == null) { return; }

            bool customChecked = rbCustom.IsChecked == true;
            bool fromToChecked = rbFromTo.IsChecked == true;

            // Custom textbox
            if (txtCustomSheets != null)
            {
                txtCustomSheets.IsEnabled = customChecked && !_isRunning;
            }

            // From-To textboxes
            if (txtFrom != null)
            {
                txtFrom.IsEnabled = fromToChecked && !_isRunning;
            }
            if (txtTo != null)
            {
                txtTo.IsEnabled = fromToChecked && !_isRunning;
            }
        }

        private void MergeFiles_Changed(object sender, RoutedEventArgs e)
        {
            if (txtGap != null && chkDeleteAfterMerge != null && chkMergeFiles != null)
            {
                bool isMerged = chkMergeFiles.IsChecked == true;
                txtGap.IsEnabled = isMerged && !_isRunning;
                chkDeleteAfterMerge.IsEnabled = isMerged && !_isRunning;
            }
        }

        // ====================================================================
        // BROWSE FOLDER (từ frmDWGExport.btn_Browser_Click)
        // ====================================================================

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.Description = "Chọn thư mục lưu file DWG";
                    dialog.SelectedPath = txtOutputFolder.Text;

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string selected = dialog.SelectedPath;

                        // Chặn chọn OldVersions hoặc root workspace (giống iLogic)
                        if (selected.Contains("OldVersions"))
                        {
                            return;
                        }

                        txtOutputFolder.Text = selected;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        // ====================================================================
        // DWG TRANSLATOR OPTIONS (từ frmDWGExport.btnOptions_ButtonClick)
        // ====================================================================

        private void BtnOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_engine != null)
                {
                    _engine.ShowTranslatorOptions();
                }
                else
                {
                    // Tạo engine tạm để mở Options
                    ExportAutoCadEngine tempEngine = new ExportAutoCadEngine(_invApp);
                    if (tempEngine.InitTranslator())
                    {
                        tempEngine.ShowTranslatorOptions();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "DWG Translator không tìm thấy.",
                            "VinTed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        // ====================================================================
        // BẮT ĐẦU XUẤT (từ frmDWGExport.btnProceed_ButtonClick + PublishDWG)
        // ====================================================================

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // --- VALIDATION (100% từ frmDWGExport.btnProceed_ButtonClick) ---

                // Kiểm tra tên file
                if (String.IsNullOrEmpty(txtBaseFileName.Text))
                {
                    txtBaseFileName.Focus();
                    return;
                }

                // Kiểm tra ký tự đặc biệt trong tên file
                char[] badCharsFile = new char[] { '*', '\\', '/', ':', '?', '"', '<', '>', '|' };
                string badFound = CheckSpecialCharacters(txtBaseFileName.Text, badCharsFile);
                if (!String.IsNullOrEmpty(badFound))
                {
                    System.Windows.MessageBox.Show(
                        "Phát hiện ký tự đặc biệt: " + badFound + "\nKý tự này không được hỗ trợ trong việc đặt tên file.",
                        "VinTed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtBaseFileName.Focus();
                    return;
                }

                // Kiểm tra thư mục
                if (String.IsNullOrEmpty(txtOutputFolder.Text))
                {
                    txtOutputFolder.Focus();
                    return;
                }

                char[] badCharsPath = new char[] { '*', '/', '?', '"', '<', '>', '|' };
                string badFoundPath = CheckSpecialCharacters(txtOutputFolder.Text, badCharsPath);
                if (!String.IsNullOrEmpty(badFoundPath))
                {
                    System.Windows.MessageBox.Show(
                        "Phát hiện ký tự đặc biệt: " + badFoundPath + "\nKý tự này không được hỗ trợ trong đường dẫn.",
                        "VinTed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtOutputFolder.Focus();
                    return;
                }

                // Tạo thư mục nếu chưa có
                if (!System.IO.Directory.Exists(txtOutputFolder.Text))
                {
                    System.IO.Directory.CreateDirectory(txtOutputFolder.Text);
                }
                else
                {
                    // Kiểm tra file DWG cũ trong thư mục (giống iLogic)
                    string[] existingFiles = System.IO.Directory.GetFiles(txtOutputFolder.Text, "*.dwg");
                    if (existingFiles.Length > 0)
                    {
                        MessageBoxResult askDelete = System.Windows.MessageBox.Show(
                            String.Format("{0} bản vẽ \".dwg\" được tìm thấy trong {1}.\n\nXoá các tệp này trước khi thực hiện lệnh?",
                                existingFiles.Length, txtOutputFolder.Text),
                            "VinTed", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (askDelete == MessageBoxResult.Yes)
                        {
                            try
                            {
                                for (int i = 0; i < existingFiles.Length; i++)
                                {
                                    System.IO.File.Delete(existingFiles[i]);
                                }
                            }
                            catch (Exception ex2)
                            {
                                System.Windows.MessageBox.Show(
                                    "Lỗi xoá tệp: " + ex2.Message,
                                    "VinTed", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                }

                // Xây dựng đường dẫn file output chính
                string outputFilePath = System.IO.Path.Combine(txtOutputFolder.Text, txtBaseFileName.Text + ".dwg");

                // Kiểm tra file đã tồn tại (giống iLogic)
                if (System.IO.File.Exists(outputFilePath))
                {
                    MessageBoxResult askReplace = System.Windows.MessageBox.Show(
                        outputFilePath + "\nTệp đã tồn tại, bạn có muốn thay thế?",
                        "VinTed", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (askReplace != MessageBoxResult.Yes)
                    {
                        return;
                    }
                    try
                    {
                        System.IO.File.Delete(outputFilePath);
                    }
                    catch (Exception ex3)
                    {
                        System.Windows.MessageBox.Show(
                            "Không thể xóa file cũ: " + ex3.Message,
                            "VinTed", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // --- KHỞI TẠO ENGINE ---
                _engine = new ExportAutoCadEngine(_invApp);
                if (!_engine.InitTranslator())
                {
                    System.Windows.MessageBox.Show(
                        "DWG Translator không tìm thấy.\nKhông tìm thấy trình bổ trợ DWG.",
                        "VinTed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _engine.ProgressChanged += Engine_ProgressChanged;

                // --- THỰC THI EXPORT ---
                SetRunningState(true);
                UpdateProgress(0, 1, "Đang chuẩn bị xuất DWG...", String.Empty);

                try
                {
                    if (_isDrawing)
                    {
                        ExportAutoCadOptions opts = ReadDrawingOptions();
                        Inventor.DrawingDocument idwDoc = (Inventor.DrawingDocument)_invApp.ActiveDocument;
                        _engine.ExecuteDrawing(idwDoc, opts, outputFilePath);

                        bool isMerged = chkMergeFiles != null && chkMergeFiles.IsChecked == true;
                        if (isMerged)
                        {
                            double gap = 100;
                            if (txtGap != null) double.TryParse(txtGap.Text, out gap);
                            bool delete = chkDeleteAfterMerge != null && chkDeleteAfterMerge.IsChecked == true;
                            _engine.MergeDwgFiles(opts.OutputFolder, outputFilePath, gap, delete);
                        }

                        FinishRun(outputFilePath, isMerged);
                    }
                    else
                    {
                        ExportAutoCadOptions opts = ReadPartAssemblyOptions();
                        _engine.ExecutePartAssembly(opts);
                        FinishRun(outputFilePath, false);
                    }
                }
                finally
                {
                    if (_engine != null)
                    {
                        _engine.ProgressChanged -= Engine_ProgressChanged;
                    }
                    SetRunningState(false);
                }
            }
            catch (Exception ex)
            {
                SetRunningState(false);
                lblProgress.Text = "Xuất DWG thất bại";
                ShowError(ex);
            }
        }

        // ====================================================================
        // KẾT THÚC
        // ====================================================================

        private void FinishRun(string outputFilePath, bool isMerged)
        {
            if (_engine != null && _engine.WasStopped)
            {
                lblProgress.Text = String.Format("Đã dừng — đã xuất {0} sheet", _engine.ExportedCount);
                lblOutput.Text = "Người dùng đã yêu cầu STOP. Có thể chạy lại phần còn lại nếu cần.";
                return;
            }

            try
            {
                if (isMerged && System.IO.File.Exists(outputFilePath))
                {
                    // Mở trực tiếp file DWG bằng AutoCAD
                    Process.Start(outputFilePath);
                }
                else if (System.IO.Directory.Exists(txtOutputFolder.Text))
                {
                    // Nếu không gộp, hoặc file không tồn tại, mở thư mục output
                    Process.Start("explorer.exe", txtOutputFolder.Text);
                }
            }
            catch (Exception) { }

            // Đóng form ngay
            this.Close();
        }

        // ====================================================================
        // STOP (từ frmDWGExport — add-in cho phép dừng giữa chừng)
        // ====================================================================

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_engine != null)
                {
                    _engine.RequestStop();
                    btnStop.IsEnabled = false;
                    lblProgress.Text = "Đã nhận STOP — đang chờ hoàn tất sheet hiện tại...";
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        // ====================================================================
        // ĐỌC OPTIONS TỪ UI
        // ====================================================================

        private ExportAutoCadOptions ReadDrawingOptions()
        {
            ExportAutoCadOptions opts = new ExportAutoCadOptions();
            opts.OutputFolder = txtOutputFolder.Text;
            opts.BaseFileName = txtBaseFileName.Text;
            opts.ExportSpace = cmbExportSpace.SelectedIndex == 0 ? "Model" : "Layout";
            opts.UseOptimizedIni = chkOptimizedIni.IsChecked == true;
            opts.DisableScreenUpdating = chkDisableScreenUpdating.IsChecked == true;
            opts.EnableSilentOperation = chkEnableSilentOperation.IsChecked == true;

            // Sheet mode
            if (rbAllSheets.IsChecked == true)
            {
                opts.SheetMode = SheetExportMode.AllSheets;
            }
            else if (rbCurrentSheet.IsChecked == true)
            {
                opts.SheetMode = SheetExportMode.CurrentSheet;
            }
            else if (rbCustom.IsChecked == true)
            {
                opts.SheetMode = SheetExportMode.Custom;
                opts.CustomSheets = txtCustomSheets.Text;
            }
            else if (rbFromTo.IsChecked == true)
            {
                opts.SheetMode = SheetExportMode.FromTo;

                int from;
                int to;
                if (!Int32.TryParse(txtFrom.Text, out from) || from < 1)
                {
                    throw new InvalidOperationException("Giá trị 'Từ' không hợp lệ.");
                }
                if (!Int32.TryParse(txtTo.Text, out to) || to < 1)
                {
                    throw new InvalidOperationException("Giá trị 'Đến' không hợp lệ.");
                }
                if (from > _sheetCount)
                {
                    throw new InvalidOperationException(
                        String.Format("'Từ' ({0}) vượt quá số sheet ({1}).", from, _sheetCount));
                }
                if (to > _sheetCount)
                {
                    throw new InvalidOperationException(
                        String.Format("'Đến' ({0}) vượt quá số sheet ({1}).", to, _sheetCount));
                }
                if (from >= to)
                {
                    throw new InvalidOperationException("'Từ' phải nhỏ hơn 'Đến'.");
                }

                opts.FromSheet = from;
                opts.ToSheet = to;
            }

            return opts;
        }

        private ExportAutoCadOptions ReadPartAssemblyOptions()
        {
            ExportAutoCadOptions opts = new ExportAutoCadOptions();
            opts.OutputFolder = txtOutputFolder.Text;
            opts.BaseFileName = txtBaseFileName.Text;
            opts.DisableScreenUpdating = chkDisableScreenUpdating.IsChecked == true;
            opts.EnableSilentOperation = chkEnableSilentOperation.IsChecked == true;
            return opts;
        }

        // ====================================================================
        // VALIDATION (từ frmDWGExport.CheckSpecialCharacters)
        // ====================================================================

        /// <summary>
        /// Kiểm tra ký tự đặc biệt. Trả về chuỗi các ký tự tìm thấy, hoặc null nếu OK.
        /// </summary>
        private string CheckSpecialCharacters(string text, char[] badChars)
        {
            List<char> found = new List<char>();
            foreach (char c in text)
            {
                for (int i = 0; i < badChars.Length; i++)
                {
                    if (c == badChars[i] && !found.Contains(c))
                    {
                        found.Add(c);
                    }
                }
            }
            if (found.Count > 0)
            {
                return String.Join(", ", found.ConvertAll<string>(delegate(char ch) { return ch.ToString(); }).ToArray());
            }
            return null;
        }

        // ====================================================================
        // PROGRESS (từ Engine callback)
        // ====================================================================

        private void Engine_ProgressChanged(object sender, ExportAutoCadProgressEventArgs e)
        {
            try
            {
                UpdateProgress(e.CurrentSheet, e.TotalSheets, e.Message, e.OutputFile);
                System.Windows.Forms.Application.DoEvents();
            }
            catch (Exception) { }
        }

        private void UpdateProgress(int current, int total, string message, string outputFile)
        {
            if (total <= 0) { total = 1; }
            double percent = Math.Max(0, Math.Min(100, (current * 100.0) / total));
            progressBar.Value = percent;
            lblPercent.Text = String.Format("{0:0}%", percent);
            lblProgress.Text = message;
            if (!String.IsNullOrEmpty(outputFile))
            {
                lblOutput.Text = outputFile;
            }
        }

        // ====================================================================
        // UI STATE
        // ====================================================================

        private void SetRunningState(bool running)
        {
            _isRunning = running;
            btnStart.IsEnabled = !running;
            btnBrowse.IsEnabled = !running;
            btnOptions.IsEnabled = !running;
            txtOutputFolder.IsEnabled = !running;
            txtBaseFileName.IsEnabled = !running;
            cmbExportSpace.IsEnabled = !running && _isDrawing;
            chkOptimizedIni.IsEnabled = !running && _isDrawing;
            btnStop.IsEnabled = running;

            // Sheet mode controls
            rbAllSheets.IsEnabled = !running && _isDrawing;
            rbCurrentSheet.IsEnabled = !running && _isDrawing;
            rbCustom.IsEnabled = !running && _isDrawing;
            rbFromTo.IsEnabled = !running && _isDrawing;

            if (!running)
            {
                ApplySheetModeUi();
            }
            else
            {
                txtCustomSheets.IsEnabled = false;
                txtFrom.IsEnabled = false;
                txtTo.IsEnabled = false;
            }
        }

        // ====================================================================
        // HELPER
        // ====================================================================

        private void ShowError(Exception ex)
        {
            System.Windows.MessageBox.Show(
                "Lỗi: " + ex.Message,
                "VinTed Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
