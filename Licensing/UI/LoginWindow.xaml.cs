using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace VinTed.Licensing.UI
{
    public partial class LoginWindow : Window
    {
        private bool _isSignUpMode;
        private System.Windows.Threading.Dispatcher _dispatcher;

        /// <summary>
        /// Event khi đăng nhập thành công — để caller biết cập nhật license.
        /// </summary>
        public event Action OnLoginSuccess;

        public LoginWindow()
        {
            InitializeComponent();
            _isSignUpMode = false;
            _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
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
