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
                    iconSmall = IconHelper.CreateIconFromIconify("mdi/find-replace", 16,
                        System.Drawing.Color.White, System.Drawing.Color.FromArgb(0, 0, 0, 0));
                    iconLarge = IconHelper.CreateIconFromIconify("mdi/find-replace", 32,
                        System.Drawing.Color.White, System.Drawing.Color.FromArgb(0, 0, 0, 0));
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

        public void Deactivate()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            _btnFindReplace = null;
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
