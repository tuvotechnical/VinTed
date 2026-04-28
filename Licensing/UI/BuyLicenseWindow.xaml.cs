using System;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VinTed.Licensing.UI
{
    public partial class BuyLicenseWindow : Window
    {
        private System.Windows.Threading.Dispatcher _dispatcher;
        private System.Windows.Threading.DispatcherTimer _pollTimer;
        private System.Windows.Threading.DispatcherTimer _countdownTimer;
        private string _currentOrderId;
        private DateTime _orderExpiresAt;
        private string _selectedPlanId;
        private bool _isPaid;

        /// <summary>
        /// Event khi mua thành công — để caller biết cập nhật license.
        /// </summary>
        public event Action OnPurchaseSuccess;

        public BuyLicenseWindow()
        {
            InitializeComponent();
            _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            _isPaid = false;

            // Mặc định chọn monthly
            _selectedPlanId = "monthly";
            HighlightSelectedPlan();
        }

        // ===== Plan Selection =====

        private void CardDaily_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _selectedPlanId = "daily";
            HighlightSelectedPlan();
            CreateOrder();
        }

        private void CardMonthly_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _selectedPlanId = "monthly";
            HighlightSelectedPlan();
            CreateOrder();
        }

        private void CardYearly_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _selectedPlanId = "yearly";
            HighlightSelectedPlan();
            CreateOrder();
        }

        private void HighlightSelectedPlan()
        {
            SolidColorBrush normalBorder = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            SolidColorBrush activeBorder = new SolidColorBrush(Color.FromRgb(0, 93, 166));
            SolidColorBrush normalBg = new SolidColorBrush(Colors.White);
            SolidColorBrush activeBg = new SolidColorBrush(Color.FromRgb(240, 247, 255));

            cardDaily.BorderBrush = _selectedPlanId == "daily" ? activeBorder : normalBorder;
            cardDaily.Background = _selectedPlanId == "daily" ? activeBg : normalBg;
            cardMonthly.BorderBrush = _selectedPlanId == "monthly" ? activeBorder : normalBorder;
            cardMonthly.Background = _selectedPlanId == "monthly" ? activeBg : normalBg;
            cardYearly.BorderBrush = _selectedPlanId == "yearly" ? activeBorder : normalBorder;
            cardYearly.Background = _selectedPlanId == "yearly" ? activeBg : normalBg;
        }

        // ===== Create Order =====

        private void CreateOrder()
        {
            StopPolling();

            string accessToken = SupabaseAuth.GetValidAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                MessageBox.Show(
                    "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.",
                    "VinTed", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close();
                return;
            }

            progressRing.IsActive = true;
            progressRing.Visibility = Visibility.Visible;
            panelPayment.Visibility = Visibility.Collapsed;
            panelSuccess.Visibility = Visibility.Collapsed;

            string planId = _selectedPlanId;
            string token = accessToken;

            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                try
                {
                    OrderInfo order = LicenseClient.CreateOrder(token, planId);

                    _dispatcher.BeginInvoke(new Action(delegate()
                    {
                        progressRing.IsActive = false;
                        progressRing.Visibility = Visibility.Collapsed;

                        if (!string.IsNullOrEmpty(order.ErrorMessage))
                        {
                            ShowPaymentError(order.ErrorMessage);
                            return;
                        }

                        if (string.IsNullOrEmpty(order.OrderId))
                        {
                            ShowPaymentError("Không tạo được đơn hàng.");
                            return;
                        }

                        // Hiển thị thông tin thanh toán
                        _currentOrderId = order.OrderId;
                        _orderExpiresAt = order.ExpiresAt;
                        _isPaid = false;

                        txtOrderCode.Text = order.OrderCode;
                        txtAmount.Text = FormatVnd(order.AmountVnd);
                        txtStatus.Text = "Đang chờ thanh toán...";
                        panelSuccess.Visibility = Visibility.Collapsed;
                        txtPaymentError.Visibility = Visibility.Collapsed;
                        panelPayment.Visibility = Visibility.Visible;

                        // Load QR image
                        LoadQrImage(order.QrUrl);

                        // Bắt đầu polling + countdown
                        StartPolling();
                        StartCountdown();
                    }));
                }
                catch (Exception ex)
                {
                    _dispatcher.BeginInvoke(new Action(delegate()
                    {
                        progressRing.IsActive = false;
                        progressRing.Visibility = Visibility.Collapsed;
                        ShowPaymentError("Lỗi: " + ex.Message);
                    }));
                }
            });
        }

        // ===== QR Image =====

        private void LoadQrImage(string qrUrl)
        {
            if (string.IsNullOrEmpty(qrUrl))
            {
                return;
            }

            try
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(qrUrl);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                imgQrCode.Source = bmp;
            }
            catch (Exception)
            {
                // Fallback: thử tải manual bằng WebClient trên background thread
                ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    try
                    {
                        using (WebClient client = new WebClient())
                        {
                            byte[] imageBytes = client.DownloadData(qrUrl);

                            _dispatcher.BeginInvoke(new Action(delegate()
                            {
                                try
                                {
                                    System.IO.MemoryStream ms =
                                        new System.IO.MemoryStream(imageBytes);
                                    BitmapImage bmp2 = new BitmapImage();
                                    bmp2.BeginInit();
                                    bmp2.StreamSource = ms;
                                    bmp2.CacheOption = BitmapCacheOption.OnLoad;
                                    bmp2.EndInit();
                                    bmp2.Freeze();
                                    imgQrCode.Source = bmp2;
                                }
                                catch (Exception) { }
                            }));
                        }
                    }
                    catch (Exception) { }
                });
            }
        }

        // ===== Polling =====

        private void StartPolling()
        {
            StopPolling();

            _pollTimer = new System.Windows.Threading.DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromMilliseconds(LicenseConfig.OrderPollIntervalMs);
            _pollTimer.Tick += PollTimer_Tick;
            _pollTimer.Start();
        }

        private void StopPolling()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Stop();
                _pollTimer.Tick -= PollTimer_Tick;
                _pollTimer = null;
            }
            StopCountdown();
        }

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            if (_isPaid || string.IsNullOrEmpty(_currentOrderId))
            {
                StopPolling();
                return;
            }

            string accessToken = SupabaseAuth.GetValidAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                return;
            }

            string orderId = _currentOrderId;
            string token = accessToken;

            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                try
                {
                    OrderStatusResult status = LicenseClient.CheckOrderStatus(token, orderId);

                    _dispatcher.BeginInvoke(new Action(delegate()
                    {
                        if (status.IsPaid)
                        {
                            _isPaid = true;
                            StopPolling();
                            ShowPaymentSuccess();
                        }
                        else if (status.Status == "expired" || status.Status == "cancelled")
                        {
                            StopPolling();
                            txtStatus.Text = "Đơn hàng đã hết hạn.";
                        }
                    }));
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi poll — sẽ thử lại lần sau
                }
            });
        }

        // ===== Countdown =====

        private void StartCountdown()
        {
            StopCountdown();

            _countdownTimer = new System.Windows.Threading.DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
        }

        private void StopCountdown()
        {
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Tick -= CountdownTimer_Tick;
                _countdownTimer = null;
            }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            if (_isPaid)
            {
                StopCountdown();
                return;
            }

            TimeSpan remaining = _orderExpiresAt - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0)
            {
                txtCountdown.Text = "(Hết hạn)";
                StopPolling();
                txtStatus.Text = "Đơn hàng đã hết hạn.";
                return;
            }

            txtCountdown.Text = String.Format("({0:D2}:{1:D2})",
                (int)remaining.TotalMinutes, remaining.Seconds);
        }

        // ===== Success =====

        private void ShowPaymentSuccess()
        {
            txtStatus.Text = "Đã thanh toán!";
            panelSuccess.Visibility = Visibility.Visible;
            btnCancel.Content = "Đóng";

            // Refresh license trên background
            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                LicenseManager.RefreshLicense();
                _dispatcher.BeginInvoke(new Action(delegate()
                {
                    if (OnPurchaseSuccess != null)
                    {
                        OnPurchaseSuccess();
                    }
                }));
            });
        }

        // ===== Error =====

        private void ShowPaymentError(string message)
        {
            txtPaymentError.Text = message;
            txtPaymentError.Visibility = Visibility.Visible;
            panelPayment.Visibility = Visibility.Visible;
        }

        // ===== Cancel =====

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            StopPolling();
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopPolling();
        }

        // ===== Helpers =====

        private string FormatVnd(int amount)
        {
            return String.Format("{0:N0}đ", amount);
        }
    }
}
