using System;
using System.Reflection;

namespace VinTed.Licensing
{
    /// <summary>
    /// Facade chính quản lý toàn bộ luồng license.
    /// Sử dụng trong StandardAddInServer và trước mỗi feature.
    /// </summary>
    public static class LicenseManager
    {
        private static LicenseInfo _currentLicense;
        private static readonly object _lock = new object();

        /// <summary>
        /// License hiện tại (đã được check lần cuối).
        /// </summary>
        public static LicenseInfo CurrentLicense
        {
            get
            {
                lock (_lock)
                {
                    return _currentLicense;
                }
            }
        }

        /// <summary>
        /// Kiểm tra license: online trước, fallback cache offline.
        /// Gọi từ background thread (method này BLOCKING).
        /// </summary>
        public static LicenseInfo CheckLicense()
        {
            LicenseInfo info = new LicenseInfo();

            try
            {
                // Bước 1: Kiểm tra đã đăng nhập chưa
                if (!SecureTokenStore.HasStoredTokens())
                {
                    info.Status = "not_logged_in";
                    lock (_lock) { _currentLicense = info; }
                    return info;
                }

                // Bước 2: Lấy access token hợp lệ (tự refresh nếu cần)
                string accessToken = SupabaseAuth.GetValidAccessToken();
                if (string.IsNullOrEmpty(accessToken))
                {
                    // Token hết hạn và không refresh được
                    // Thử dùng cache offline
                    LicenseInfo cached = LicenseCache.LoadCache();
                    if (cached != null && cached.IsActive)
                    {
                        lock (_lock) { _currentLicense = cached; }
                        return cached;
                    }

                    info.Status = "token_expired";
                    lock (_lock) { _currentLicense = info; }
                    return info;
                }

                // Bước 3: Gọi verify-license online
                string deviceId = DeviceFingerprint.GetDeviceHash();
                string appVersion = GetCurrentVersion();

                info = LicenseClient.VerifyLicense(accessToken, deviceId, appVersion);

                // Bước 4: Xử lý kết quả
                if (info.Status == "token_expired")
                {
                    // Thử refresh token rồi verify lại
                    string refreshToken = SecureTokenStore.GetRefreshToken();
                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        AuthResult refreshResult = SupabaseAuth.RefreshToken(refreshToken);
                        if (refreshResult.Success)
                        {
                            info = LicenseClient.VerifyLicense(
                                refreshResult.AccessToken, deviceId, appVersion);
                        }
                    }
                }

                if (info.Status == "offline")
                {
                    // Không có mạng → dùng cache
                    LicenseInfo cached = LicenseCache.LoadCache();
                    if (cached != null && cached.IsActive)
                    {
                        lock (_lock) { _currentLicense = cached; }
                        return cached;
                    }
                    // Không có cache hợp lệ
                    info.Status = "offline_no_cache";
                }
                else if (info.IsActive)
                {
                    // Online thành công → cập nhật cache
                    info.UserEmail = SecureTokenStore.GetEmail() ?? "";
                    LicenseCache.SaveCache(info);
                }

                lock (_lock) { _currentLicense = info; }
            }
            catch (Exception)
            {
                // Lỗi bất kỳ → thử cache
                LicenseInfo cached = LicenseCache.LoadCache();
                if (cached != null && cached.IsActive)
                {
                    lock (_lock) { _currentLicense = cached; }
                    return cached;
                }
                info.Status = "error";
                lock (_lock) { _currentLicense = info; }
            }

            return info;
        }

        /// <summary>
        /// Kiểm tra license đã active chưa (dùng cache memory, không gọi network).
        /// Gọi trước mỗi feature. Nếu chưa check lần nào thì trả về false.
        /// </summary>
        public static bool RequireActive()
        {
            lock (_lock)
            {
                if (_currentLicense == null)
                {
                    return false;
                }
                return _currentLicense.IsActive;
            }
        }

        /// <summary>
        /// Kiểm tra đã đăng nhập chưa (có token lưu trữ).
        /// </summary>
        public static bool IsLoggedIn()
        {
            return SecureTokenStore.HasStoredTokens();
        }

        /// <summary>
        /// Đăng xuất: xóa token, cache, reset license.
        /// </summary>
        public static void Logout()
        {
            SupabaseAuth.SignOut();
            lock (_lock)
            {
                _currentLicense = null;
            }
        }

        /// <summary>
        /// Cập nhật license sau khi mua thành công (gọi verify lại).
        /// Gọi từ background thread.
        /// </summary>
        public static LicenseInfo RefreshLicense()
        {
            return CheckLicense();
        }

        /// <summary>
        /// Lấy version hiện tại của add-in.
        /// </summary>
        private static string GetCurrentVersion()
        {
            try
            {
                Version ver = Assembly.GetExecutingAssembly().GetName().Version;
                return String.Format("{0}.{1}.{2}", ver.Major, ver.Minor, ver.Build);
            }
            catch (Exception)
            {
                return "0.0.0";
            }
        }
    }
}
