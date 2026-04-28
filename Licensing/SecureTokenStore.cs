using System;
using System.Security.Cryptography;
using System.Text;

namespace VinTed.Licensing
{
    /// <summary>
    /// Lưu trữ token xác thực an toàn bằng Windows DPAPI.
    /// File được mã hóa bằng ProtectedData — chỉ user Windows hiện tại mới đọc được.
    /// Lưu tại: %AppData%\VinTed\auth.dat
    /// </summary>
    public static class SecureTokenStore
    {
        private const string AuthFileName = "auth.dat";

        /// <summary>
        /// Lưu tokens sau khi đăng nhập thành công.
        /// </summary>
        public static void SaveTokens(string accessToken, string refreshToken, string email, string userId)
        {
            try
            {
                string folder = LicenseConfig.GetDataFolder();
                string filePath = System.IO.Path.Combine(folder, AuthFileName);

                // Format: accessToken|refreshToken|email|userId|savedAtUtc
                string plainText = String.Format("{0}|{1}|{2}|{3}|{4}",
                    accessToken, refreshToken, email, userId,
                    DateTime.UtcNow.ToString("o"));

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(
                    plainBytes, null, DataProtectionScope.CurrentUser);

                System.IO.File.WriteAllBytes(filePath, encryptedBytes);
            }
            catch (Exception)
            {
                // Im lặng — không crash Inventor vì lưu token thất bại
            }
        }

        /// <summary>
        /// Đọc tokens đã lưu.
        /// Trả về mảng [accessToken, refreshToken, email, userId] hoặc null nếu chưa lưu.
        /// </summary>
        public static string[] LoadTokens()
        {
            try
            {
                string folder = LicenseConfig.GetDataFolder();
                string filePath = System.IO.Path.Combine(folder, AuthFileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return null;
                }

                byte[] encryptedBytes = System.IO.File.ReadAllBytes(filePath);
                byte[] plainBytes = ProtectedData.Unprotect(
                    encryptedBytes, null, DataProtectionScope.CurrentUser);

                string plainText = Encoding.UTF8.GetString(plainBytes);
                string[] parts = plainText.Split('|');

                // Cần ít nhất 4 phần: accessToken, refreshToken, email, userId
                if (parts.Length < 4)
                {
                    return null;
                }

                return new string[] { parts[0], parts[1], parts[2], parts[3] };
            }
            catch (CryptographicException)
            {
                // Token bị hỏng hoặc user khác — xóa file
                DeleteTokens();
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Lấy access token đã lưu. Trả về null nếu chưa đăng nhập.
        /// </summary>
        public static string GetAccessToken()
        {
            string[] tokens = LoadTokens();
            if (tokens != null && tokens.Length > 0 && !string.IsNullOrEmpty(tokens[0]))
            {
                return tokens[0];
            }
            return null;
        }

        /// <summary>
        /// Lấy refresh token đã lưu. Trả về null nếu chưa đăng nhập.
        /// </summary>
        public static string GetRefreshToken()
        {
            string[] tokens = LoadTokens();
            if (tokens != null && tokens.Length > 1 && !string.IsNullOrEmpty(tokens[1]))
            {
                return tokens[1];
            }
            return null;
        }

        /// <summary>
        /// Lấy email đã lưu.
        /// </summary>
        public static string GetEmail()
        {
            string[] tokens = LoadTokens();
            if (tokens != null && tokens.Length > 2)
            {
                return tokens[2];
            }
            return null;
        }

        /// <summary>
        /// Lấy user ID đã lưu.
        /// </summary>
        public static string GetUserId()
        {
            string[] tokens = LoadTokens();
            if (tokens != null && tokens.Length > 3)
            {
                return tokens[3];
            }
            return null;
        }

        /// <summary>
        /// Xóa tokens (đăng xuất).
        /// </summary>
        public static void DeleteTokens()
        {
            try
            {
                string folder = LicenseConfig.GetDataFolder();
                string filePath = System.IO.Path.Combine(folder, AuthFileName);

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
        /// Kiểm tra xem đã có token lưu trữ hay chưa.
        /// </summary>
        public static bool HasStoredTokens()
        {
            string[] tokens = LoadTokens();
            return tokens != null && tokens.Length >= 4
                && !string.IsNullOrEmpty(tokens[0]);
        }
    }
}
