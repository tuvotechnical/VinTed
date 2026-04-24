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
                string url = _updateInfo.DownloadUrl;
                if (string.IsNullOrEmpty(url))
                {
                    url = _updateInfo.HtmlUrl;
                }

                if (!string.IsNullOrEmpty(url))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/tuvotechnical/VinTed/releases",
                        UseShellExecute = true
                    });
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi mở trình duyệt: " + ex.Message,
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
