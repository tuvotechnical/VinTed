using System;

namespace VinTed.Licensing
{
    /// <summary>
    /// Model chứa thông tin license trả về từ server.
    /// </summary>
    public class LicenseInfo
    {
        private string _status;
        private string _plan;
        private DateTime _expiresAt;
        private DateTime _serverTime;
        private int _maxDevices;
        private string _userEmail;
        private string _userId;

        public LicenseInfo()
        {
            _status = "inactive";
            _plan = "";
            _expiresAt = DateTime.MinValue;
            _serverTime = DateTime.UtcNow;
            _maxDevices = 1;
            _userEmail = "";
            _userId = "";
        }

        /// <summary>
        /// Trạng thái license: "active", "inactive", "expired"
        /// </summary>
        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }

        /// <summary>
        /// Gói license: "daily", "monthly", "yearly"
        /// </summary>
        public string Plan
        {
            get { return _plan; }
            set { _plan = value; }
        }

        /// <summary>
        /// Thời điểm hết hạn (UTC)
        /// </summary>
        public DateTime ExpiresAt
        {
            get { return _expiresAt; }
            set { _expiresAt = value; }
        }

        /// <summary>
        /// Thời gian server trả về (UTC) — dùng để phát hiện chỉnh giờ máy
        /// </summary>
        public DateTime ServerTime
        {
            get { return _serverTime; }
            set { _serverTime = value; }
        }

        /// <summary>
        /// Số thiết bị tối đa được phép
        /// </summary>
        public int MaxDevices
        {
            get { return _maxDevices; }
            set { _maxDevices = value; }
        }

        /// <summary>
        /// Email người dùng
        /// </summary>
        public string UserEmail
        {
            get { return _userEmail; }
            set { _userEmail = value; }
        }

        /// <summary>
        /// User ID (UUID từ Supabase)
        /// </summary>
        public string UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        /// <summary>
        /// License còn hoạt động hay không
        /// </summary>
        public bool IsActive
        {
            get
            {
                return string.Equals(_status, "active", StringComparison.OrdinalIgnoreCase)
                    && _expiresAt > DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Số ngày còn lại
        /// </summary>
        public int DaysRemaining
        {
            get
            {
                if (_expiresAt <= DateTime.UtcNow) return 0;
                return (int)(_expiresAt - DateTime.UtcNow).TotalDays;
            }
        }
    }

    /// <summary>
    /// Kết quả đăng nhập từ Supabase Auth.
    /// </summary>
    public class AuthResult
    {
        private bool _success;
        private string _accessToken;
        private string _refreshToken;
        private int _expiresIn;
        private string _userId;
        private string _email;
        private string _errorMessage;
        private bool _needsEmailConfirmation;

        public AuthResult()
        {
            _success = false;
            _accessToken = "";
            _refreshToken = "";
            _expiresIn = 0;
            _userId = "";
            _email = "";
            _errorMessage = "";
            _needsEmailConfirmation = false;
        }

        public bool Success
        {
            get { return _success; }
            set { _success = value; }
        }

        public string AccessToken
        {
            get { return _accessToken; }
            set { _accessToken = value; }
        }

        public string RefreshToken
        {
            get { return _refreshToken; }
            set { _refreshToken = value; }
        }

        public int ExpiresIn
        {
            get { return _expiresIn; }
            set { _expiresIn = value; }
        }

        public string UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        /// <summary>
        /// True nếu đăng ký thành công nhưng cần xác nhận email trước khi đăng nhập.
        /// </summary>
        public bool NeedsEmailConfirmation
        {
            get { return _needsEmailConfirmation; }
            set { _needsEmailConfirmation = value; }
        }
    }

    /// <summary>
    /// Thông tin đơn hàng trả về từ create-order.
    /// </summary>
    public class OrderInfo
    {
        private string _orderId;
        private string _orderCode;
        private int _amountVnd;
        private string _qrUrl;
        private DateTime _expiresAt;
        private string _errorMessage;

        public OrderInfo()
        {
            _orderId = "";
            _orderCode = "";
            _amountVnd = 0;
            _qrUrl = "";
            _expiresAt = DateTime.MinValue;
            _errorMessage = "";
        }

        public string OrderId
        {
            get { return _orderId; }
            set { _orderId = value; }
        }

        public string OrderCode
        {
            get { return _orderCode; }
            set { _orderCode = value; }
        }

        public int AmountVnd
        {
            get { return _amountVnd; }
            set { _amountVnd = value; }
        }

        public string QrUrl
        {
            get { return _qrUrl; }
            set { _qrUrl = value; }
        }

        public DateTime ExpiresAt
        {
            get { return _expiresAt; }
            set { _expiresAt = value; }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }
    }

    /// <summary>
    /// Kết quả kiểm tra trạng thái đơn hàng.
    /// </summary>
    public class OrderStatusResult
    {
        private string _status;
        private DateTime _licenseExpiresAt;

        public OrderStatusResult()
        {
            _status = "pending";
            _licenseExpiresAt = DateTime.MinValue;
        }

        /// <summary>
        /// Trạng thái: "pending", "paid", "expired", "cancelled", "underpaid"
        /// </summary>
        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public DateTime LicenseExpiresAt
        {
            get { return _licenseExpiresAt; }
            set { _licenseExpiresAt = value; }
        }

        public bool IsPaid
        {
            get { return string.Equals(_status, "paid", StringComparison.OrdinalIgnoreCase); }
        }
    }
}
