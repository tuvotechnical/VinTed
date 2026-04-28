using System;
using System.Windows;
using System.Windows.Media;

namespace VinTed.Licensing.UI
{
    public partial class AccountWindow : Window
    {
        /// <summary>
        /// Event khi đăng xuất — để caller biết reset UI.
        /// </summary>
        public event Action OnLogout;

        public AccountWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "VinTed Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshDisplay()
        {
            // Email
            string email = SecureTokenStore.GetEmail();
            txtEmail.Text = !string.IsNullOrEmpty(email) ? email : "(Không rõ)";

            // Device
            txtDevice.Text = DeviceFingerprint.GetDeviceName() + " (máy này)";

            // License info
            LicenseInfo license = LicenseManager.CurrentLicense;

            if (license == null || !LicenseManager.IsLoggedIn())
            {
                txtStatus.Text = "Chưa đăng nhập";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
                txtPlan.Text = "—";
                txtExpires.Text = "—";
                txtDaysLeft.Text = "—";
                return;
            }

            if (license.IsActive)
            {
                txtStatus.Text = "✅ Đang hoạt động";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else if (string.Equals(license.Status, "expired", StringComparison.OrdinalIgnoreCase))
            {
                txtStatus.Text = "❌ Đã hết hạn";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
            else
            {
                txtStatus.Text = "⚠ Chưa kích hoạt";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 127, 23));
            }

            // Plan name
            string plan = license.Plan;
            if (!string.IsNullOrEmpty(plan))
            {
                string planLower = plan.ToLowerInvariant();
                if (planLower == "daily") txtPlan.Text = "VinTed 1 ngày";
                else if (planLower == "monthly") txtPlan.Text = "VinTed 1 tháng";
                else if (planLower == "yearly") txtPlan.Text = "VinTed 1 năm";
                else txtPlan.Text = plan;
            }
            else
            {
                txtPlan.Text = "—";
            }

            // Expires
            if (license.ExpiresAt > DateTime.MinValue)
            {
                txtExpires.Text = license.ExpiresAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            }
            else
            {
                txtExpires.Text = "—";
            }

            // Days remaining
            int days = license.DaysRemaining;
            if (license.IsActive)
            {
                txtDaysLeft.Text = String.Format("{0} ngày", days);
                txtDaysLeft.Foreground = new SolidColorBrush(Color.FromRgb(0, 93, 166));
            }
            else
            {
                txtDaysLeft.Text = "Đã hết hạn";
                txtDaysLeft.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
        }

        private void BtnBuyLicense_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!LicenseManager.IsLoggedIn())
                {
                    MessageBox.Show("Vui lòng đăng nhập trước khi mua license.",
                        "VinTed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                BuyLicenseWindow buyWindow = new BuyLicenseWindow();
                buyWindow.OnPurchaseSuccess += delegate()
                {
                    RefreshDisplay();
                };
                buyWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "VinTed Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult confirm = MessageBox.Show(
                    "Bạn có chắc chắn muốn đăng xuất?",
                    "VinTed",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    LicenseManager.Logout();

                    if (OnLogout != null)
                    {
                        OnLogout();
                    }

                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "VinTed Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
