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
        private ButtonDefinition _btnInsertPlus;
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
                    iconSmall = IconHelper.CreateIconFromIconify("fluent/find-replace-24-filled", 16,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                    iconLarge = IconHelper.CreateIconFromIconify("fluent/find-replace-24-filled", 32,
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
                    iconCopyHatchSmall = IconHelper.CreateIconFromIconify("mdi/format-paint", 16,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                    iconCopyHatchLarge = IconHelper.CreateIconFromIconify("mdi/format-paint", 32,
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

                // Tạo ButtonDefinition cho Insert Plus
                stdole.IPictureDisp iconInsertSmall = null;
                stdole.IPictureDisp iconInsertLarge = null;
                try
                {
                    iconInsertSmall = IconHelper.CreateIconFromIconify("fluent/add-circle-24-filled", 16,
                        System.Drawing.Color.FromArgb(0, 93, 166), System.Drawing.Color.FromArgb(0, 0, 0, 0));
                    iconInsertLarge = IconHelper.CreateIconFromIconify("fluent/add-circle-24-filled", 32,
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

                // Đăng ký vào Ribbon nếu firstTime
                if (firstTime)
                {
                    AddToRibbon();
                }

                // Kiểm tra cập nhật (background, không block Inventor)
                CheckForUpdateAsync();
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
                                try
                                {
                                    Updater.UpdateNotificationWindow win =
                                        new Updater.UpdateNotificationWindow(result);
                                    win.Show();
                                }
                                catch (Exception) { }
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
                Ribbon drawingRibbon = _invApp.UserInterfaceManager.Ribbons["Drawing"];

                // Tạo Tab riêng cho VinTed
                RibbonTab vinTedTab = null;
                try
                {
                    vinTedTab = drawingRibbon.RibbonTabs["VinTed"];
                }
                catch (Exception)
                {
                    vinTedTab = drawingRibbon.RibbonTabs.Add(
                        "VinTed", "VinTed_Tab",
                        "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}");
                }

                // Tạo Panel
                RibbonPanel panel = null;
                try
                {
                    panel = vinTedTab.RibbonPanels["Text Tools"];
                }
                catch (Exception)
                {
                    panel = vinTedTab.RibbonPanels.Add(
                        "Text Tools", "VinTed_TextTools",
                        "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}");
                }

                panel.CommandControls.AddButton(_btnFindReplace);

                // Tạo Panel Drawing Tools cho Copy Hatch
                RibbonPanel panelDrawing = null;
                try
                {
                    panelDrawing = vinTedTab.RibbonPanels["Drawing Tools"];
                }
                catch (Exception)
                {
                    panelDrawing = vinTedTab.RibbonPanels.Add(
                        "Drawing Tools", "VinTed_DrawingTools",
                        "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}");
                }

                panelDrawing.CommandControls.AddButton(_btnCopyHatch);

                // --- TẠO TAB CHO MÔI TRƯỜNG ASSEMBLY ---
                Ribbon assemblyRibbon = _invApp.UserInterfaceManager.Ribbons["Assembly"];
                
                RibbonTab asmVinTedTab = null;
                try
                {
                    asmVinTedTab = assemblyRibbon.RibbonTabs["VinTed"];
                }
                catch (Exception)
                {
                    asmVinTedTab = assemblyRibbon.RibbonTabs.Add(
                        "VinTed", "VinTed_AsmTab",
                        "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}");
                }

                // Tạo Panel Assembly Tools cho Insert Plus
                RibbonPanel panelAssembly = null;
                try
                {
                    panelAssembly = asmVinTedTab.RibbonPanels["Assembly Tools"];
                }
                catch (Exception)
                {
                    panelAssembly = asmVinTedTab.RibbonPanels.Add(
                        "Assembly Tools", "VinTed_AssemblyTools",
                        "{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}");
                }

                panelAssembly.CommandControls.AddButton(_btnInsertPlus);
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

        public void Deactivate()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            _btnFindReplace = null;
            _btnCopyHatch = null;
            _btnInsertPlus = null;
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
