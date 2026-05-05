using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Inventor;

namespace VinTed
{
    /// <summary>
    /// Entry point cho VinTed Add-in.
    /// Đăng ký Ribbon Button vào Ribbon Tab "VinTed" của môi trường Drawing.
    /// Tích hợp auto-update checker khi khởi động.
    /// </summary>
    [ComVisible(true)]
    [Guid("D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90")]
    [ProgId("VinTed.StandardAddInServer")]
    public class StandardAddInServer : ApplicationAddInServer
    {
        private Application _invApp;
        private ButtonDefinition _btnFindReplace;
        private ButtonDefinition _btnCopyHatch;
        private ButtonDefinition _btnExportCad;
        private ButtonDefinition _btnInsertPlus;
        private ButtonDefinition _btnUpdate;
        private ButtonDefinition _btnAccount;
        private static string _addinFolder;
        private System.Windows.Threading.Dispatcher _uiDispatcher;

        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            try
            {
                // Đăng ký AssemblyResolve để CLR tìm ModernWpf.dll cạnh VinTed.dll
                _addinFolder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

                // Lưu Dispatcher của UI thread (Inventor main STA thread)
                _uiDispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

                _invApp = addInSiteObject.Application;

                // Tạo icon cho nút Find & Replace
                stdole.IPictureDisp iconSmall = null;
                stdole.IPictureDisp iconLarge = null;
                try
                {
                    string iconPath = System.IO.Path.Combine(_addinFolder, "Assets", "search.svg");
                    iconSmall = IconHelper.CreateIconFromSvgFile(iconPath, 16,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                    iconLarge = IconHelper.CreateIconFromSvgFile(iconPath, 32,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                }
                catch (Exception) { }

                // Tạo ButtonDefinition
                ControlDefinitions ctrlDefs = _invApp.CommandManager.ControlDefinitions;
                _btnFindReplace = ctrlDefs.AddButtonDefinition(
                    "Find && Replace",
                    "VinTed_FindReplace",
                    CommandTypesEnum.kEditMaskCmdType,
                    "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}",
                    "Tìm và thay thế Text trong bản vẽ",
                    "VinTed Find & Replace\nTìm kiếm và thay thế hàng loạt nội dung Text trong Drawing.",
                    iconSmall,
                    iconLarge);

                _btnFindReplace.OnExecute += new ButtonDefinitionSink_OnExecuteEventHandler(OnFindReplace_Execute);

                // Tạo ButtonDefinition cho Copy Hatch
                stdole.IPictureDisp iconCopyHatchSmall = null;
                stdole.IPictureDisp iconCopyHatchLarge = null;
                try
                {
                    string iconPath = System.IO.Path.Combine(_addinFolder, "Assets", "paintbrush.svg");
                    iconCopyHatchSmall = IconHelper.CreateIconFromSvgFile(iconPath, 16,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                    iconCopyHatchLarge = IconHelper.CreateIconFromSvgFile(iconPath, 32,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                }
                catch (Exception) { }

                _btnCopyHatch = ctrlDefs.AddButtonDefinition(
                    "Copy Hatch",
                    "VinTed_CopyHatch",
                    CommandTypesEnum.kEditMaskCmdType,
                    "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}",
                    "Copy Hatch Pattern giữa các chi tiết trong Section View",
                    "VinTed Copy Hatch\nSao chép pattern mặt cắt từ chi tiết mẫu sang chi tiết đích.",
                    iconCopyHatchSmall,
                    iconCopyHatchLarge);

                _btnCopyHatch.OnExecute += new ButtonDefinitionSink_OnExecuteEventHandler(OnCopyHatch_Execute);

                // Tạo ButtonDefinition cho Export CAD
                stdole.IPictureDisp iconExportCadSmall = null;
                stdole.IPictureDisp iconExportCadLarge = null;
                try
                {
                    string iconPath = System.IO.Path.Combine(_addinFolder, "Assets", "export-cad.svg");
                    iconExportCadSmall = IconHelper.CreateIconFromSvgFile(iconPath, 16,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                    iconExportCadLarge = IconHelper.CreateIconFromSvgFile(iconPath, 32,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                }
                catch (Exception) { }

                _btnExportCad = ctrlDefs.AddButtonDefinition(
                    "Export CAD",
                    "VinTed_ExportCAD",
                    CommandTypesEnum.kFileOperationsCmdType,
                    "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}",
                    "Xuất Drawing sang AutoCAD DWG có tiến trình và STOP",
                    "VinTed Export CAD\nXuất từng sheet sang DWG, theo dõi tiến trình và dừng an toàn.",
                    iconExportCadSmall,
                    iconExportCadLarge);

                _btnExportCad.OnExecute += new ButtonDefinitionSink_OnExecuteEventHandler(OnExportCad_Execute);

                // Tạo ButtonDefinition cho Insert Plus
                stdole.IPictureDisp iconInsertSmall = null;
                stdole.IPictureDisp iconInsertLarge = null;
                try
                {
                    string iconPath = System.IO.Path.Combine(_addinFolder, "Assets", "plus.svg");
                    iconInsertSmall = IconHelper.CreateIconFromSvgFile(iconPath, 16,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                    iconInsertLarge = IconHelper.CreateIconFromSvgFile(iconPath, 32,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                }
                catch (Exception) { }

                _btnInsertPlus = ctrlDefs.AddButtonDefinition(
                    "Insert Plus+",
                    "VinTed_InsertPlus",
                    CommandTypesEnum.kEditMaskCmdType,
                    "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}",
                    "Copy và lắp ráp tự động bu-lông, đai ốc hàng loạt",
                    "VinTed Insert Plus+\nSao chép cụm chi tiết phần cứng và tự động tạo ràng buộc Insert.",
                    iconInsertSmall,
                    iconInsertLarge);

                _btnInsertPlus.OnExecute += new ButtonDefinitionSink_OnExecuteEventHandler(OnInsertPlus_Execute);

                // Tạo ButtonDefinition cho Update
                stdole.IPictureDisp iconUpdateSmall = null;
                stdole.IPictureDisp iconUpdateLarge = null;
                try
                {
                    string iconPath = System.IO.Path.Combine(_addinFolder, "Assets", "synchronize.svg");
                    iconUpdateSmall = IconHelper.CreateIconFromSvgFile(iconPath, 16,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                    iconUpdateLarge = IconHelper.CreateIconFromSvgFile(iconPath, 32,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                }
                catch (Exception) { }

                _btnUpdate = ctrlDefs.AddButtonDefinition(
                    "Check for Updates",
                    "VinTed_CheckUpdate",
                    CommandTypesEnum.kEditMaskCmdType,
                    "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}",
                    "Kiểm tra phiên bản mới của VinTed",
                    "VinTed Update\nTự động kiểm tra và cập nhật add-in lên phiên bản mới nhất.",
                    iconUpdateSmall,
                    iconUpdateLarge);

                _btnUpdate.OnExecute += new ButtonDefinitionSink_OnExecuteEventHandler(OnUpdate_Execute);

                // Tạo ButtonDefinition cho Account
                stdole.IPictureDisp iconAccountSmall = null;
                stdole.IPictureDisp iconAccountLarge = null;
                try
                {
                    string iconPath = System.IO.Path.Combine(_addinFolder, "Assets", "manager.svg");
                    iconAccountSmall = IconHelper.CreateIconFromSvgFile(iconPath, 16,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                    iconAccountLarge = IconHelper.CreateIconFromSvgFile(iconPath, 32,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                }
                catch (Exception) { }

                _btnAccount = ctrlDefs.AddButtonDefinition(
                    "Account",
                    "VinTed_Account",
                    CommandTypesEnum.kEditMaskCmdType,
                    "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}",
                    "Quản lý tài khoản và license VinTed",
                    "VinTed Account\nĐăng nhập, mua license, xem trạng thái tài khoản.",
                    iconAccountSmall,
                    iconAccountLarge);

                _btnAccount.OnExecute += new ButtonDefinitionSink_OnExecuteEventHandler(OnAccount_Execute);

                // Đăng ký vào Ribbon nếu firstTime
                if (firstTime)
                {
                    AddToRibbon();
                }

                // Kiểm tra cập nhật (background, không block Inventor)
                CheckForUpdateAsync();

                // Kiểm tra license (background, không block Inventor)
                CheckLicenseAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Lỗi khi khởi tạo VinTed Add-in: " + ex.Message,
                    "VinTed Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Kiểm tra cập nhật trên background thread.
        /// Nếu có version mới, dispatch về UI thread để hiện dialog.
        /// </summary>
        private void CheckForUpdateAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                try
                {
                    // Delay 5 giây để Inventor khởi động xong
                    Thread.Sleep(5000);

                    Updater.UpdateCheckResult result = Updater.UpdateChecker.CheckForUpdate();
                    if (result.HasUpdate)
                    {
                        // Kiểm tra user đã skip version này chưa
                        if (Updater.UpdateChecker.IsVersionSkipped(result.LatestVersion))
                        {
                            return;
                        }

                        // Dispatch về UI thread để hiện dialog
                        _uiDispatcher.BeginInvoke(
                            new Action(delegate()
                            {
                                ShowUpdateResultOnUi(result, false);
                            }));
                    }
                }
                catch (Exception)
                {
                    // Im lặng — update check không được crash Inventor
                }
            });
        }

        private void AddToRibbon()
        {
            try
            {
                // Mảng các môi trường (environments) cần có tab VinTed
                string[] envs = new string[] { "ZeroDoc", "Part", "Assembly", "Drawing", "Presentation" };
                
                foreach (string envName in envs)
                {
                    try
                    {
                        Ribbon ribbon = _invApp.UserInterfaceManager.Ribbons[envName];
                        if (ribbon == null) continue;

                        // Tạo hoặc lấy Tab VinTed
                        RibbonTab vinTedTab = null;
                        try { vinTedTab = ribbon.RibbonTabs["VinTed"]; }
                        catch { vinTedTab = ribbon.RibbonTabs.Add("VinTed", "VinTed_Tab_" + envName, "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}"); }

                        // Thêm nút Update vào mỗi tab
                        RibbonPanel panelAbout = null;
                        try { panelAbout = vinTedTab.RibbonPanels["About"]; }
                        catch { panelAbout = vinTedTab.RibbonPanels.Add("About", "VinTed_About_" + envName, "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}"); }
                        
                        try { panelAbout.CommandControls.AddButton(_btnUpdate); } catch { }
                        try { panelAbout.CommandControls.AddButton(_btnAccount); } catch { }

                        // Thêm các công cụ khác theo môi trường
                        if (envName == "Drawing")
                        {
                            RibbonPanel panelText = null;
                            try { panelText = vinTedTab.RibbonPanels["Text Tools"]; }
                            catch { panelText = vinTedTab.RibbonPanels.Add("Text Tools", "VinTed_TextTools", "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}"); }
                            try { panelText.CommandControls.AddButton(_btnFindReplace); } catch { }

                            RibbonPanel panelDrawing = null;
                            try { panelDrawing = vinTedTab.RibbonPanels["Drawing Tools"]; }
                            catch { panelDrawing = vinTedTab.RibbonPanels.Add("Drawing Tools", "VinTed_DrawingTools", "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}"); }
                            try { panelDrawing.CommandControls.AddButton(_btnCopyHatch); } catch { }
                            try { panelDrawing.CommandControls.AddButton(_btnExportCad); } catch { }
                        }
                        else if (envName == "Assembly")
                        {
                            RibbonPanel panelAssembly = null;
                            try { panelAssembly = vinTedTab.RibbonPanels["Assembly Tools"]; }
                            catch { panelAssembly = vinTedTab.RibbonPanels.Add("Assembly Tools", "VinTed_AssemblyTools", "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}"); }
                            try { panelAssembly.CommandControls.AddButton(_btnInsertPlus); } catch { }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Lỗi đăng ký Ribbon: " + ex.Message,
                    "VinTed Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void OnFindReplace_Execute(NameValueMap context)
        {
            try
            {
                Document activeDoc = _invApp.ActiveDocument;
                if (activeDoc == null || activeDoc.DocumentType != DocumentTypeEnum.kDrawingDocumentObject)
                {
                    System.Windows.MessageBox.Show(
                        "Tính năng này chỉ hoạt động trong môi trường Drawing (.idw / .dwg).",
                        "VinTed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (!Licensing.LicenseManager.RequireActive())
                {
                    ShowLicenseRequired();
                    return;
                }

                DrawingDocument drawDoc = (DrawingDocument)activeDoc;
                FindReplace.FindReplaceWindow window = new FindReplace.FindReplaceWindow(_invApp, drawDoc);
                window.Show();
            }
            catch (Exception ex)
            {
                string msg = "Lỗi: " + ex.Message;
                if (ex.InnerException != null)
                {
                    msg = msg + "\n\nInner: " + ex.InnerException.Message;
                }
                msg = msg + "\n\nStack: " + ex.StackTrace;
                System.Windows.MessageBox.Show(
                    msg,
                    "VinTed Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void OnCopyHatch_Execute(NameValueMap context)
        {
            try
            {
                Document activeDoc = _invApp.ActiveDocument;
                if (activeDoc == null || activeDoc.DocumentType != DocumentTypeEnum.kDrawingDocumentObject)
                {
                    System.Windows.MessageBox.Show(
                        "Tính năng này chỉ hoạt động trong môi trường Drawing (.idw / .dwg).",
                        "VinTed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (!Licensing.LicenseManager.RequireActive())
                {
                    ShowLicenseRequired();
                    return;
                }

                DrawingDocument drawDoc = (DrawingDocument)activeDoc;
                CopyHatch.CopyHatchWindow window = new CopyHatch.CopyHatchWindow(_invApp, drawDoc);
                window.Show();
            }
            catch (Exception ex)
            {
                string msg = "Lỗi: " + ex.Message;
                if (ex.InnerException != null)
                {
                    msg = msg + "\n\nInner: " + ex.InnerException.Message;
                }
                msg = msg + "\n\nStack: " + ex.StackTrace;
                System.Windows.MessageBox.Show(
                    msg,
                    "VinTed Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void OnExportCad_Execute(NameValueMap context)
        {
            try
            {
                Document activeDoc = _invApp.ActiveDocument;
                if (activeDoc == null || activeDoc.DocumentType != DocumentTypeEnum.kDrawingDocumentObject)
                {
                    System.Windows.MessageBox.Show(
                        "Tính năng Export CAD chỉ hoạt động trong môi trường Drawing (.idw / .dwg).",
                        "VinTed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (!Licensing.LicenseManager.RequireActive())
                {
                    ShowLicenseRequired();
                    return;
                }

                DrawingDocument drawDoc = (DrawingDocument)activeDoc;
                ExportAutoCAD.ExportAutoCadWindow window = new ExportAutoCAD.ExportAutoCadWindow(_invApp, drawDoc);
                
                try
                {
                    System.Windows.Interop.WindowInteropHelper helper = new System.Windows.Interop.WindowInteropHelper(window);
                    helper.Owner = new IntPtr(_invApp.MainFrameHWND);
                }
                catch { }

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                string msg = "Lỗi: " + ex.Message;
                if (ex.InnerException != null)
                {
                    msg = msg + "\n\nInner: " + ex.InnerException.Message;
                }
                msg = msg + "\n\nStack: " + ex.StackTrace;
                System.Windows.MessageBox.Show(
                    msg,
                    "VinTed Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void OnInsertPlus_Execute(NameValueMap context)
        {
            try
            {
                Document activeDoc = _invApp.ActiveDocument;
                if (activeDoc == null || activeDoc.DocumentType != DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    System.Windows.MessageBox.Show(
                        "Tính năng Insert Plus+ chỉ hoạt động trong môi trường Assembly (.iam).",
                        "VinTed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if (!Licensing.LicenseManager.RequireActive())
                {
                    ShowLicenseRequired();
                    return;
                }

                AssemblyDocument asmDoc = (AssemblyDocument)activeDoc;
                InsertPlus.InsertPlusWindow window = new InsertPlus.InsertPlusWindow(_invApp, asmDoc);
                window.Show();
            }
            catch (Exception ex)
            {
                string msg = "Lỗi: " + ex.Message;
                if (ex.InnerException != null)
                {
                    msg = msg + "\n\nInner: " + ex.InnerException.Message;
                }
                msg = msg + "\n\nStack: " + ex.StackTrace;
                System.Windows.MessageBox.Show(
                    msg,
                    "VinTed Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void OnUpdate_Execute(NameValueMap context)
        {
            try
            {
                // Gọi CheckForUpdate
                ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    try
                    {
                        Updater.UpdateCheckResult result = Updater.UpdateChecker.CheckForUpdate();
                        _uiDispatcher.BeginInvoke(new Action(delegate()
                        {
                            ShowUpdateResultOnUi(result, true);
                        }));
                    }
                    catch (Exception ex)
                    {
                        _uiDispatcher.BeginInvoke(new Action(delegate()
                        {
                            System.Windows.MessageBox.Show("Lỗi khi kiểm tra cập nhật: " + ex.Message, "VinTed Error",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "VinTed Error");
            }
        }

        private void ShowUpdateResultOnUi(Updater.UpdateCheckResult result, bool showNoUpdateMessage)
        {
            try
            {
                if (result == null)
                {
                    if (showNoUpdateMessage)
                    {
                        System.Windows.MessageBox.Show(
                            "Không thể đọc thông tin cập nhật. Vui lòng kiểm tra kết nối Internet và thử lại.",
                            "VinTed Update",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                    }
                    return;
                }

                if (result.HasUpdate)
                {
                    Updater.UpdateNotificationWindow win = new Updater.UpdateNotificationWindow(result);
                    win.Show();
                }
                else if (showNoUpdateMessage)
                {
                    System.Windows.MessageBox.Show(
                        "Bạn đang sử dụng phiên bản VinTed mới nhất (" + result.CurrentVersion + ").",
                        "VinTed Update",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    System.Windows.MessageBox.Show(
                        "Lỗi khi hiển thị thông tin cập nhật: " + ex.Message,
                        "VinTed Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Kiểm tra license trên background thread.
        /// Nếu chưa đăng nhập hoặc hết hạn, hiện cửa sổ.
        /// </summary>
        private void CheckLicenseAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                try
                {
                    Thread.Sleep(3000);
                    Licensing.LicenseInfo license = Licensing.LicenseManager.CheckLicense();

                    if (license != null && !license.IsActive)
                    {
                        // Không tự động mở window — chỉ log để khi dùng feature sẽ yêu cầu
                    }
                }
                catch (Exception)
                {
                    // Im lặng — license check không được crash Inventor
                }
            });
        }

        private void OnAccount_Execute(NameValueMap context)
        {
            try
            {
                if (!Licensing.LicenseManager.IsLoggedIn())
                {
                    Licensing.UI.LoginWindow loginWin = new Licensing.UI.LoginWindow();
                    loginWin.OnLoginSuccess += delegate()
                    {
                        // Sau khi đăng nhập → check license + mở Account window
                        ThreadPool.QueueUserWorkItem(delegate(object state)
                        {
                            Licensing.LicenseManager.CheckLicense();
                            _uiDispatcher.BeginInvoke(new Action(delegate()
                            {
                                Licensing.UI.AccountWindow accWin = new Licensing.UI.AccountWindow();
                                accWin.Show();
                            }));
                        });
                    };
                    loginWin.Show();
                }
                else
                {
                    Licensing.UI.AccountWindow accWin = new Licensing.UI.AccountWindow();
                    accWin.Show();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "VinTed Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Hiện thông báo yêu cầu license khi tính năng bị khóa.
        /// </summary>
        private void ShowLicenseRequired()
        {
            if (!Licensing.LicenseManager.IsLoggedIn())
            {
                System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                    "Tính năng này yêu cầu license VinTed.\nBạn muốn đăng nhập ngay?",
                    "VinTed License",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    Licensing.UI.LoginWindow loginWin = new Licensing.UI.LoginWindow();
                    loginWin.OnLoginSuccess += delegate()
                    {
                        ThreadPool.QueueUserWorkItem(delegate(object state)
                        {
                            Licensing.LicenseManager.CheckLicense();
                        });
                    };
                    loginWin.Show();
                }
            }
            else
            {
                System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                    "License VinTed chưa kích hoạt hoặc đã hết hạn.\nBạn muốn mua/gia hạn license?",
                    "VinTed License",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    Licensing.UI.BuyLicenseWindow buyWin = new Licensing.UI.BuyLicenseWindow();
                    buyWin.Show();
                }
            }
        }

        public void Deactivate()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            _btnFindReplace = null;
            _btnCopyHatch = null;
            _btnExportCad = null;
            _btnInsertPlus = null;
            _btnUpdate = null;
            _btnAccount = null;
            _invApp = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void ExecuteCommand(int commandID)
        {
            // Not used
        }

        public object Automation
        {
            get { return null; }
        }

        /// <summary>
        /// Giải quyết dependencies (ModernWpf.dll...) từ thư mục chứa VinTed.dll.
        /// Inventor host process không tự probe thư mục add-in.
        /// </summary>
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string assemblyName = new AssemblyName(args.Name).Name;
                string dllPath = System.IO.Path.Combine(_addinFolder, assemblyName + ".dll");
                if (System.IO.File.Exists(dllPath))
                {
                    return Assembly.LoadFrom(dllPath);
                }
            }
            catch (Exception) { }
            return null;
        }
    }
}
