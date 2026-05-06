using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VinTed.Licensing.UI
{
    public partial class LoginWindow : Window
    {
        private bool _isSignUpMode;
        private System.Windows.Threading.Dispatcher _dispatcher;

        // ===== Win32 Keyboard Hook =====
        private const int WH_KEYBOARD = 2;
        private const int VK_SPACE = 0x20;
        private const int VK_RETURN = 0x0D;
        private const int VK_V = 0x56;
        private const int VK_C = 0x43;
        private const int VK_X = 0x58;
        private const int VK_A = 0x41;
        private const int VK_Z = 0x5A;

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
        private HookProc _hookProcDelegate;

        /// <summary>
        /// Event khi đăng nhập thành công — để caller biết cập nhật license.
        /// </summary>
        public event Action OnLoginSuccess;

        public LoginWindow()
        {
            InitializeComponent();
            _isSignUpMode = false;
            _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            
            Loaded += OnWindowLoaded;
            Closed += OnWindowClosed;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _hookProcDelegate = new HookProc(KeyboardHookCallback);
            _hookId = SetWindowsHookEx(WH_KEYBOARD, _hookProcDelegate, IntPtr.Zero, GetCurrentThreadId());
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsActive)
            {
                int vkCode = wParam.ToInt32();
                bool isKeyDown = ((long)lParam & 0x80000000L) == 0;
                bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

                if (isKeyDown)
                {
                    IInputElement focused = Keyboard.FocusedElement;
                    TextBox tb = focused as TextBox;
                    PasswordBox pb = focused as PasswordBox;

                    if (tb != null || pb != null)
                    {
                        if (isCtrl && vkCode == VK_V) // Ctrl+V
                        {
                            if (tb != null) tb.Paste();
                            if (pb != null) pb.Paste();
                            return (IntPtr)1;
                        }
                        else if (isCtrl && vkCode == VK_C) // Ctrl+C
                        {
                            if (tb != null) tb.Copy();
                            return (IntPtr)1;
                        }
                        else if (isCtrl && vkCode == VK_X) // Ctrl+X
                        {
                            if (tb != null) tb.Cut();
                            return (IntPtr)1;
                        }
                        else if (isCtrl && vkCode == VK_A) // Ctrl+A
                        {
                            if (tb != null) tb.SelectAll();
                            if (pb != null) pb.SelectAll();
                            return (IntPtr)1;
                        }
                        else if (isCtrl && vkCode == VK_Z) // Ctrl+Z
                        {
                            if (tb != null) tb.Undo();
                            return (IntPtr)1;
                        }
                        else if (vkCode == VK_SPACE && !isCtrl)
                        {
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
                            }
                            else if (pb != null)
                            {
                                pb.Password += " ";
                            }
                            return (IntPtr)1;
                        }
                        else if (vkCode == VK_RETURN && !isCtrl)
                        {
                            BtnSubmit_Click(null, null);
                            return (IntPtr)1;
                        }
                    }
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSubmit_Click(sender, e);
            }
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text;
            if (email != null) email = email.Trim();

            string password = txtPassword.Password;

            // Validation
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập email và mật khẩu.");
                return;
            }

            if (password.Length < 6)
            {
                ShowError("Mật khẩu phải có ít nhất 6 ký tự.");
                return;
            }

            if (_isSignUpMode)
            {
                string confirmPassword = txtConfirmPassword.Password;
                if (password != confirmPassword)
                {
                    ShowError("Mật khẩu xác nhận không khớp.");
                    return;
                }
            }

            // Disable form + show loading
            SetLoading(true);
            HideError();

            // Gọi API trên background thread
            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                try
                {
                    AuthResult result;

                    if (_isSignUpMode)
                    {
                        result = SupabaseAuth.SignUp(email, password);
                    }
                    else
                    {
                        result = SupabaseAuth.SignIn(email, password);
                    }

                    _dispatcher.BeginInvoke(new Action(delegate()
                    {
                        SetLoading(false);

                        if (result.Success && result.NeedsEmailConfirmation)
                        {
                            // Đăng ký thành công, cần xác nhận email
                            ShowSuccess("✅ Đăng ký thành công!\nVui lòng kiểm tra email và nhấn link xác nhận,\nsau đó quay lại đây để đăng nhập.");

                            // Tự động chuyển về chế độ Đăng nhập
                            _isSignUpMode = false;
                            txtTitle.Text = "Đăng nhập";
                            btnSubmit.Content = "ĐĂNG NHẬP";
                            txtToggleLabel.Text = "Chưa có tài khoản?";
                            btnToggle.Content = "Đăng ký";
                            lblConfirmPassword.Visibility = Visibility.Collapsed;
                            txtConfirmPassword.Visibility = Visibility.Collapsed;

                            // Giữ email đã nhập, xóa password
                            txtPassword.Clear();
                        }
                        else if (result.Success)
                        {
                            // Đăng nhập thành công → fire event và đóng
                            if (OnLoginSuccess != null)
                            {
                                OnLoginSuccess();
                            }
                            this.Close();
                        }
                        else
                        {
                            // Hiện lỗi thân thiện
                            string errorMsg = TranslateError(result.ErrorMessage);
                            ShowError(errorMsg);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    _dispatcher.BeginInvoke(new Action(delegate()
                    {
                        SetLoading(false);
                        ShowError("Lỗi kết nối: " + ex.Message);
                    }));
                }
            });
        }

        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            _isSignUpMode = !_isSignUpMode;
            HideError();

            if (_isSignUpMode)
            {
                txtTitle.Text = "Tạo tài khoản";
                btnSubmit.Content = "ĐĂNG KÝ";
                txtToggleLabel.Text = "Đã có tài khoản?";
                btnToggle.Content = "Đăng nhập";
                lblConfirmPassword.Visibility = Visibility.Visible;
                txtConfirmPassword.Visibility = Visibility.Visible;
            }
            else
            {
                txtTitle.Text = "Đăng nhập";
                btnSubmit.Content = "ĐĂNG NHẬP";
                txtToggleLabel.Text = "Chưa có tài khoản?";
                btnToggle.Content = "Đăng ký";
                lblConfirmPassword.Visibility = Visibility.Collapsed;
                txtConfirmPassword.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowError(string message)
        {
            txtError.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D32F2F"));
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void ShowSuccess(string message)
        {
            txtError.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2E7D32"));
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            txtError.Text = "";
            txtError.Visibility = Visibility.Collapsed;
        }

        private void SetLoading(bool isLoading)
        {
            btnSubmit.IsEnabled = !isLoading;
            txtEmail.IsEnabled = !isLoading;
            txtPassword.IsEnabled = !isLoading;
            txtConfirmPassword.IsEnabled = !isLoading;
            btnToggle.IsEnabled = !isLoading;

            if (isLoading)
            {
                progressRing.IsActive = true;
                progressRing.Visibility = Visibility.Visible;
            }
            else
            {
                progressRing.IsActive = false;
                progressRing.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Dịch lỗi Supabase sang tiếng Việt thân thiện.
        /// </summary>
        private string TranslateError(string error)
        {
            if (string.IsNullOrEmpty(error)) return "Đã xảy ra lỗi.";

            string lower = error.ToLowerInvariant();
            if (lower.Contains("invalid login credentials"))
            {
                return "Email hoặc mật khẩu không đúng.";
            }
            if (lower.Contains("user already registered"))
            {
                return "Email này đã được đăng ký.";
            }
            if (lower.Contains("password should be at least"))
            {
                return "Mật khẩu phải có ít nhất 6 ký tự.";
            }
            if (lower.Contains("unable to validate email"))
            {
                return "Địa chỉ email không hợp lệ.";
            }
            if (lower.Contains("email not confirmed"))
            {
                return "Vui lòng xác nhận email trước khi đăng nhập.";
            }
            if (lower.Contains("rate limit"))
            {
                return "Quá nhiều yêu cầu. Vui lòng thử lại sau.";
            }
            return error;
        }
    }
}
