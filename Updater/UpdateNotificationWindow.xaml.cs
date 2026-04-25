using System;
using System.Diagnostics;
using System.Windows;

namespace VinTed.Updater
{
    /// <summary>
    /// Code-behind cho cửa sổ thông báo cập nhật.
    /// Hiển thị version mới, release notes, và cho phép tải về.
    /// </summary>
    public partial class UpdateNotificationWindow : Window
    {
        private UpdateCheckResult _updateInfo;

        public UpdateNotificationWindow(UpdateCheckResult updateInfo)
        {
            InitializeComponent();
            _updateInfo = updateInfo;
            PopulateUI();
        }

        private void PopulateUI()
        {
            try
            {
                TxtCurrentVersion.Text = _updateInfo.CurrentVersion;
                TxtLatestVersion.Text = _updateInfo.LatestVersion;

                if (!string.IsNullOrEmpty(_updateInfo.ReleaseNotes))
                {
                    TxtReleaseNotes.Text = _updateInfo.ReleaseNotes;
                }
                else
                {
                    TxtReleaseNotes.Text = "Không có thông tin chi tiết.";
                }

                TxtSubHeader.Text = String.Format(
                    "VinTed v{0} đã sẵn sàng tải về",
                    _updateInfo.LatestVersion);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi hiển thị thông tin update: " + ex.Message,
                    "VinTed Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string scriptUrl = "https://raw.githubusercontent.com/tuvotechnical/VinTed/main/install.ps1";
                string command = String.Format("-NoProfile -ExecutionPolicy Bypass -Command \"irm {0} | iex\"", scriptUrl);
                
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = command,
                    UseShellExecute = true
                };
                
                Process.Start(psi);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi chạy cập nhật: " + ex.Message,
                    "VinTed Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ChkSkipVersion.IsChecked == true && _updateInfo != null)
                {
                    UpdateChecker.SkipVersion(_updateInfo.LatestVersion);
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi: " + ex.Message,
                    "VinTed Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
