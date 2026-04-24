using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace VinTed.FindReplace
{
    /// <summary>
    /// Code-behind cho FindReplaceWindow.
    /// Kết nối UI WPF (ModernWpf Light Theme) → FindReplaceEngine (core logic bảo toàn từ iLogic).
    /// 
    /// GHI CHÚ QUAN TRỌNG — Space Key trong Inventor non-modal (.Show()):
    /// Inventor host process sở hữu message pump và dùng TranslateAccelerator
    /// để intercept keyboard shortcut TRƯỚC khi WPF nhận message.
    /// WPF PreviewKeyDown KHÔNG BAO GIỜ thấy phím Space.
    /// Giải pháp: cài Win32 keyboard hook (WH_KEYBOARD) ở thread level.
    /// Hook này chạy trước TranslateAccelerator, cho phép ta suppress Space
    /// và tự chèn vào TextBox.
    /// </summary>
    public partial class FindReplaceWindow : Window
    {
        private readonly FindReplaceEngine _engine;
        private bool _suppressAllCheck;
        private bool _isExpanded = true;

        // ===== Win32 Keyboard Hook (chống Inventor nuốt Space) =====
        private const int WH_KEYBOARD = 2;
        private const int VK_SPACE = 0x20;
        private const int VK_RETURN = 0x0D;

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private IntPtr _hookId = IntPtr.Zero;
        private HookProc _hookProcDelegate; // Giữ reference để GC không thu hồi delegate

        public FindReplaceWindow(Inventor.Application invApp, Inventor.DrawingDocument drawDoc)
        {
            InitializeComponent();
            _engine = new FindReplaceEngine(invApp, drawDoc);

            Loaded += OnWindowLoaded;
            Closed += OnWindowClosed;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Cài keyboard hook ngay khi window load xong
            _hookProcDelegate = new HookProc(KeyboardHookCallback);
            _hookId = SetWindowsHookEx(WH_KEYBOARD, _hookProcDelegate, IntPtr.Zero, GetCurrentThreadId());
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            // Gỡ hook khi đóng window
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Low-level keyboard hook callback.
        /// Chạy TRƯỚC Inventor TranslateAccelerator.
        /// Nếu Space được nhấn khi TextBox đang có focus → tự chèn space, suppress message.
        /// </summary>
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsActive)
            {
                int vkCode = wParam.ToInt32();

                // Bit 31 của lParam: 0 = key down, 1 = key up
                bool isKeyDown = ((long)lParam & 0x80000000L) == 0;

                if (isKeyDown && vkCode == VK_SPACE)
                {
                    IInputElement focused = Keyboard.FocusedElement;
                    TextBox tb = focused as TextBox;
                    if (tb != null)
                    {
                        int caret = tb.CaretIndex;
                        int selLen = tb.SelectionLength;
                        int selStart = tb.SelectionStart;
                        if (selLen > 0)
                        {
                            tb.Text = tb.Text.Remove(selStart, selLen);
                            caret = selStart;
                        }
                        tb.Text = tb.Text.Insert(caret, " ");
                        tb.CaretIndex = caret + 1;
                        // Return 1 = suppress message, Inventor không nhận được
                        return (IntPtr)1;
                    }
                }
                else if (isKeyDown && vkCode == VK_RETURN)
                {
                    IInputElement focused = Keyboard.FocusedElement;
                    TextBox tb = focused as TextBox;
                    if (tb != null)
                    {
                        BtnFind_Click(tb, new RoutedEventArgs());
                        return (IntPtr)1;
                    }
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        #region Find / Replace Handlers

        private void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SyncOptionsToEngine();
                string result = _engine.FindNext(txtFind.Text);
                if (!String.IsNullOrEmpty(result))
                {
                    lblStatus.Text = result;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Lỗi: " + ex.Message,
                    "VinTed Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnReplace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string result = _engine.ReplaceCurrent(txtFind.Text, txtReplace.Text);
                if (!String.IsNullOrEmpty(result))
                {
                    lblStatus.Text = result;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Lỗi: " + ex.Message,
                    "VinTed Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SyncOptionsToEngine();
                string result = _engine.ReplaceAll(txtFind.Text, txtReplace.Text);
                if (!String.IsNullOrEmpty(result))
                {
                    lblStatus.Text = result;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Lỗi: " + ex.Message,
                    "VinTed Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Checkbox Logic

        private void SyncOptionsToEngine()
        {
            ScanOptions opt = _engine.Options;
            opt.GeneralNotes = chkGeneralNotes.IsChecked == true;
            opt.LeaderNotes = chkLeaderNotes.IsChecked == true;
            opt.TitleBlocks = chkTitleBlocks.IsChecked == true;
            opt.SketchedSymbols = chkSketchedSymbols.IsChecked == true;
            opt.Dimensions = chkDimensions.IsChecked == true;
            opt.HoleThreadNotes = chkHoleThread.IsChecked == true;
            opt.ChamferNotes = chkChamfer.IsChecked == true;
            opt.BendNotes = chkBend.IsChecked == true;
            opt.PunchNotes = chkPunch.IsChecked == true;
            opt.ViewLabels = chkViewLabels.IsChecked == true;
            opt.PartsLists = chkPartsLists.IsChecked == true;
            opt.CustomTables = chkCustomTables.IsChecked == true;
            opt.RevisionTables = chkRevisionTables.IsChecked == true;
            opt.HoleTables = chkHoleTables.IsChecked == true;
            opt.Balloons = chkBalloons.IsChecked == true;
            opt.FeatureControlFrames = chkFCF.IsChecked == true;
            opt.SurfaceTextureSymbols = chkSurface.IsChecked == true;
            opt.SketchTextBoxes = chkSketchText.IsChecked == true;
        }

        private void ChkAll_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressAllCheck || !IsLoaded) return;

            bool val = chkAll.IsChecked == true;
            _suppressAllCheck = true;
            chkGeneralNotes.IsChecked = val;
            chkLeaderNotes.IsChecked = val;
            chkTitleBlocks.IsChecked = val;
            chkSketchedSymbols.IsChecked = val;
            chkDimensions.IsChecked = val;
            chkHoleThread.IsChecked = val;
            chkChamfer.IsChecked = val;
            chkBend.IsChecked = val;
            chkPunch.IsChecked = val;
            chkViewLabels.IsChecked = val;
            chkPartsLists.IsChecked = val;
            chkCustomTables.IsChecked = val;
            chkRevisionTables.IsChecked = val;
            chkHoleTables.IsChecked = val;
            chkBalloons.IsChecked = val;
            chkFCF.IsChecked = val;
            chkSurface.IsChecked = val;
            chkSketchText.IsChecked = val;
            _suppressAllCheck = false;
            SyncOptionsToEngine();
            _engine.InvalidateCache();
        }

        private void ChkItem_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressAllCheck || !IsLoaded) return;

            _suppressAllCheck = true;
            bool allChecked = chkGeneralNotes.IsChecked == true
                && chkLeaderNotes.IsChecked == true
                && chkTitleBlocks.IsChecked == true
                && chkSketchedSymbols.IsChecked == true
                && chkDimensions.IsChecked == true
                && chkHoleThread.IsChecked == true
                && chkChamfer.IsChecked == true
                && chkBend.IsChecked == true
                && chkPunch.IsChecked == true
                && chkViewLabels.IsChecked == true
                && chkPartsLists.IsChecked == true
                && chkCustomTables.IsChecked == true
                && chkRevisionTables.IsChecked == true
                && chkHoleTables.IsChecked == true
                && chkBalloons.IsChecked == true
                && chkFCF.IsChecked == true
                && chkSurface.IsChecked == true
                && chkSketchText.IsChecked == true;

            chkAll.IsChecked = allChecked;
            _suppressAllCheck = false;
            SyncOptionsToEngine();
            _engine.InvalidateCache();
        }

        #endregion

        #region Expand/Collapse

        private void ToggleExpand_Click(object sender, MouseButtonEventArgs e)
        {
            _isExpanded = !_isExpanded;

            if (_isExpanded)
            {
                pnlOptions.Visibility = Visibility.Visible;
                DoubleAnimation rotAnim = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(200)));
                icoExpandRotate.BeginAnimation(RotateTransform.AngleProperty, rotAnim);
            }
            else
            {
                pnlOptions.Visibility = Visibility.Collapsed;
                DoubleAnimation rotAnim = new DoubleAnimation(-90, new Duration(TimeSpan.FromMilliseconds(200)));
                icoExpandRotate.BeginAnimation(RotateTransform.AngleProperty, rotAnim);
            }
        }

        #endregion
    }
}
