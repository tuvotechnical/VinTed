using System;
using System.Security.Cryptography;
using System.Text;

namespace VinTed.Licensing
{
    /// <summary>
    /// Cache license offline bằng DPAPI.
    /// Cho phép dùng khi không có internet trong grace period.
    /// Phát hiện chỉnh giờ máy bằng server_time.
    /// File: %AppData%\VinTed\license_cache.dat
    /// </summary>
    public static class LicenseCache
    {
        private const string CacheFileName = "license_cache.dat";

        /// <summary>
        /// Lưu license info vào cache (mã hóa DPAPI).
        /// </summary>
        public static void SaveCache(LicenseInfo info)
        {
            try
            {
                string folder = LicenseConfig.GetDataFolder();
                string filePath = System.IO.Path.Combine(folder, CacheFileName);

                // Format: status|plan|expiresAtUtc|serverTimeUtc|maxDevices|cachedAtLocalUtc|email
                string plainText = String.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}",
                    info.Status,
                    info.Plan,
                    info.ExpiresAt.ToString("o"),
                    info.ServerTime.ToString("o"),
                    info.MaxDevices,
                    DateTime.UtcNow.ToString("o"),
                    info.UserEmail);

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(
                    plainBytes, null, DataProtectionScope.CurrentUser);

                System.IO.File.WriteAllBytes(filePath, encryptedBytes);
            }
            catch (Exception)
            {
                // Im lặng
            }
        }

        /// <summary>
        /// Đọc license từ cache.
        /// Trả về null nếu cache không tồn tại, bị hỏng, hoặc hết grace period.
        /// </summary>
        public static LicenseInfo LoadCache()
        {
            try
            {
                string folder = LicenseConfig.GetDataFolder();
                string filePath = System.IO.Path.Combine(folder, CacheFileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return null;
                }

                byte[] encryptedBytes = System.IO.File.ReadAllBytes(filePath);
                byte[] plainBytes = ProtectedData.Unprotect(
                    encryptedBytes, null, DataProtectionScope.CurrentUser);

                string plainText = Encoding.UTF8.GetString(plainBytes);
                string[] parts = plainText.Split('|');

                if (parts.Length < 6)
                {
                    return null;
                }

                LicenseInfo info = new LicenseInfo();
                info.Status = parts[0];
                info.Plan = parts[1];

                DateTime expiresAt;
                if (DateTime.TryParse(parts[2], null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out expiresAt))
                {
                    info.ExpiresAt = expiresAt;
                }

                DateTime serverTime;
                if (DateTime.TryParse(parts[3], null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out serverTime))
                {
                    info.ServerTime = serverTime;
                }

                int maxDevices;
                if (int.TryParse(parts[4], out maxDevices))
                {
                    info.MaxDevices = maxDevices;
                }

                DateTime cachedAtLocal;
                if (DateTime.TryParse(parts[5], null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out cachedAtLocal))
                {
                    // Phát hiện chỉnh lùi giờ máy
                    if (DateTime.UtcNow < cachedAtLocal.AddMinutes(-5))
                    {
                        // Giờ máy bị lùi so với lúc cache → không tin tưởng
                        return null;
                    }

                    // Kiểm tra grace period
                    int graceHours = GetGraceHours(info.Plan);
                    TimeSpan elapsed = DateTime.UtcNow - cachedAtLocal;
                    if (elapsed.TotalHours > graceHours)
                    {
                        // Quá grace period → bắt buộc online
                        return null;
                    }
                }
                else
                {
                    return null;
                }

                if (parts.Length > 6)
                {
                    info.UserEmail = parts[6];
                }

                return info;
            }
            catch (CryptographicException)
            {
                ClearCache();
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Xóa cache (khi đăng xuất hoặc cache bị hỏng).
        /// </summary>
        public static void ClearCache()
        {
            try
            {
                string folder = LicenseConfig.GetDataFolder();
                string filePath = System.IO.Path.Combine(folder, CacheFileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception)
            {
                // Im lặng
            }
        }

        /// <summary>
        /// Lấy grace period (giờ) theo gói license.
        /// </summary>
        private static int GetGraceHours(string plan)
        {
            if (string.IsNullOrEmpty(plan))
            {
                return LicenseConfig.DailyGraceHours;
            }

            string planLower = plan.ToLowerInvariant();
            if (planLower == "yearly")
            {
                return LicenseConfig.YearlyGraceHours;
            }
            if (planLower == "monthly")
            {
                return LicenseConfig.MonthlyGraceHours;
            }
            return LicenseConfig.DailyGraceHours;
        }
    }
}
