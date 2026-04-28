using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace VinTed.Licensing
{
    /// <summary>
    /// Gọi Supabase Auth REST API để đăng nhập/đăng ký.
    /// Chỉ dùng anon key (public). KHÔNG dùng service_role key.
    /// </summary>
    public static class SupabaseAuth
    {
        /// <summary>
        /// Đăng ký tài khoản mới bằng email/password.
        /// </summary>
        public static AuthResult SignUp(string email, string password)
        {
            AuthResult result = new AuthResult();
            try
            {
                string url = LicenseConfig.SupabaseUrl + LicenseConfig.AuthSignUpPath;
                string body = String.Format(
                    "{{\"email\":\"{0}\",\"password\":\"{1}\"}}",
                    EscapeJsonString(email),
                    EscapeJsonString(password));

                string response = PostJson(url, body, null);

                // Kiểm tra lỗi
                string errorMsg = ExtractJsonValue(response, "msg");
                if (string.IsNullOrEmpty(errorMsg))
                {
                    errorMsg = ExtractJsonValue(response, "message");
                }
                string errorCode = ExtractJsonValue(response, "error_code");

                if (!string.IsNullOrEmpty(errorCode))
                {
                    result.Success = false;
                    result.ErrorMessage = errorMsg ?? "Đăng ký thất bại";
                    return result;
                }

                // Parse tokens
                result.AccessToken = ExtractJsonValue(response, "access_token");
                result.RefreshToken = ExtractJsonValue(response, "refresh_token");
                result.Email = email;

                // Parse user id
                string userId = ExtractNestedUserId(response);
                result.UserId = userId ?? "";

                // Parse expires_in
                string expiresInStr = ExtractJsonNumber(response, "expires_in");
                if (!string.IsNullOrEmpty(expiresInStr))
                {
                    int expiresIn;
                    if (int.TryParse(expiresInStr, out expiresIn))
                    {
                        result.ExpiresIn = expiresIn;
                    }
                }

                if (!string.IsNullOrEmpty(result.AccessToken))
                {
                    // Có access_token → đăng ký + tự động đăng nhập (email confirm tắt)
                    result.Success = true;
                    SecureTokenStore.SaveTokens(
                        result.AccessToken, result.RefreshToken,
                        result.Email, result.UserId);
                }
                else if (!string.IsNullOrEmpty(result.UserId))
                {
                    // Có user.id nhưng không có access_token → cần xác nhận email
                    result.Success = true;
                    result.NeedsEmailConfirmation = true;
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = "Đăng ký thất bại. Vui lòng thử lại.";
                }
            }
            catch (WebException ex)
            {
                result.Success = false;
                result.ErrorMessage = ParseWebExceptionError(ex);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// Đăng nhập bằng email/password.
        /// </summary>
        public static AuthResult SignIn(string email, string password)
        {
            AuthResult result = new AuthResult();
            try
            {
                string url = LicenseConfig.SupabaseUrl + LicenseConfig.AuthSignInPath;
                string body = String.Format(
                    "{{\"email\":\"{0}\",\"password\":\"{1}\"}}",
                    EscapeJsonString(email),
                    EscapeJsonString(password));

                string response = PostJson(url, body, null);

                // Kiểm tra lỗi
                string errorMsg = ExtractJsonValue(response, "error_description");
                if (string.IsNullOrEmpty(errorMsg))
                {
                    errorMsg = ExtractJsonValue(response, "msg");
                }
                string error = ExtractJsonValue(response, "error");

                if (!string.IsNullOrEmpty(error))
                {
                    result.Success = false;
                    result.ErrorMessage = errorMsg ?? error;
                    return result;
                }

                // Parse tokens
                result.AccessToken = ExtractJsonValue(response, "access_token");
                result.RefreshToken = ExtractJsonValue(response, "refresh_token");
                result.Email = email;

                // Parse user id
                string userId = ExtractNestedUserId(response);
                result.UserId = userId ?? "";

                // Parse expires_in
                string expiresInStr = ExtractJsonNumber(response, "expires_in");
                if (!string.IsNullOrEmpty(expiresInStr))
                {
                    int expiresIn;
                    if (int.TryParse(expiresInStr, out expiresIn))
                    {
                        result.ExpiresIn = expiresIn;
                    }
                }

                result.Success = !string.IsNullOrEmpty(result.AccessToken);

                if (result.Success)
                {
                    SecureTokenStore.SaveTokens(
                        result.AccessToken, result.RefreshToken,
                        result.Email, result.UserId);
                }
            }
            catch (WebException ex)
            {
                result.Success = false;
                result.ErrorMessage = ParseWebExceptionError(ex);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// Refresh JWT token bằng refresh_token.
        /// </summary>
        public static AuthResult RefreshToken(string refreshToken)
        {
            AuthResult result = new AuthResult();
            try
            {
                string url = LicenseConfig.SupabaseUrl + LicenseConfig.AuthRefreshPath;
                string body = String.Format(
                    "{{\"refresh_token\":\"{0}\"}}",
                    EscapeJsonString(refreshToken));

                string response = PostJson(url, body, null);

                string error = ExtractJsonValue(response, "error");
                if (!string.IsNullOrEmpty(error))
                {
                    result.Success = false;
                    result.ErrorMessage = ExtractJsonValue(response, "error_description") ?? error;
                    return result;
                }

                result.AccessToken = ExtractJsonValue(response, "access_token");
                result.RefreshToken = ExtractJsonValue(response, "refresh_token");

                string userId = ExtractNestedUserId(response);
                result.UserId = userId ?? "";

                // Lấy email từ user object
                string email = ExtractJsonValue(response, "email");
                result.Email = email ?? "";

                result.Success = !string.IsNullOrEmpty(result.AccessToken);

                if (result.Success)
                {
                    SecureTokenStore.SaveTokens(
                        result.AccessToken,
                        result.RefreshToken,
                        result.Email,
                        result.UserId);
                }
            }
            catch (WebException ex)
            {
                result.Success = false;
                result.ErrorMessage = ParseWebExceptionError(ex);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// Đăng xuất: xóa tokens lưu trữ.
        /// </summary>
        public static void SignOut()
        {
            SecureTokenStore.DeleteTokens();
            LicenseCache.ClearCache();
        }

        /// <summary>
        /// Lấy access token hợp lệ. Tự động refresh nếu cần.
        /// Trả về null nếu chưa đăng nhập hoặc refresh thất bại.
        /// </summary>
        public static string GetValidAccessToken()
        {
            string accessToken = SecureTokenStore.GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                return null;
            }

            // Kiểm tra JWT hết hạn chưa (decode payload)
            if (IsJwtExpired(accessToken))
            {
                string refreshToken = SecureTokenStore.GetRefreshToken();
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return null;
                }

                AuthResult refreshResult = RefreshToken(refreshToken);
                if (refreshResult.Success)
                {
                    return refreshResult.AccessToken;
                }
                return null;
            }

            return accessToken;
        }

        // ===== Helper Methods =====

        /// <summary>
        /// Gửi POST request JSON đến Supabase.
        /// </summary>
        private static string PostJson(string url, string jsonBody, string bearerToken)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json");
                client.Headers.Add("apikey", LicenseConfig.SupabaseAnonKey);

                if (!string.IsNullOrEmpty(bearerToken))
                {
                    client.Headers.Add("Authorization", "Bearer " + bearerToken);
                }

                client.Encoding = Encoding.UTF8;
                return client.UploadString(url, "POST", jsonBody);
            }
        }

        /// <summary>
        /// Kiểm tra JWT đã hết hạn chưa (decode Base64 payload).
        /// </summary>
        private static bool IsJwtExpired(string jwt)
        {
            try
            {
                string[] parts = jwt.Split('.');
                if (parts.Length < 2) return true;

                // Decode Base64Url payload
                string payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                int padding = 4 - (payload.Length % 4);
                if (padding < 4)
                {
                    payload = payload + new string('=', padding);
                }

                byte[] payloadBytes = Convert.FromBase64String(payload);
                string payloadJson = Encoding.UTF8.GetString(payloadBytes);

                // Parse "exp" field
                string expStr = ExtractJsonNumber(payloadJson, "exp");
                if (string.IsNullOrEmpty(expStr)) return true;

                long expUnix;
                if (!long.TryParse(expStr, out expUnix)) return true;

                DateTime expTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddSeconds(expUnix);

                // Hết hạn nếu còn dưới 60 giây
                return expTime < DateTime.UtcNow.AddSeconds(60);
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>
        /// Parse lỗi từ WebException response body.
        /// </summary>
        private static string ParseWebExceptionError(WebException ex)
        {
            try
            {
                if (ex.Response != null)
                {
                    using (System.IO.Stream stream = ex.Response.GetResponseStream())
                    {
                        if (stream != null)
                        {
                            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
                            {
                                string body = reader.ReadToEnd();
                                string msg = ExtractJsonValue(body, "msg");
                                if (!string.IsNullOrEmpty(msg)) return msg;
                                msg = ExtractJsonValue(body, "error_description");
                                if (!string.IsNullOrEmpty(msg)) return msg;
                                msg = ExtractJsonValue(body, "message");
                                if (!string.IsNullOrEmpty(msg)) return msg;
                                return body;
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            return ex.Message;
        }

        /// <summary>
        /// Trích xuất user.id từ nested JSON (Supabase trả về user object bên trong).
        /// Pattern: "user":{"id":"uuid",...}
        /// </summary>
        private static string ExtractNestedUserId(string json)
        {
            // Tìm "user" block rồi lấy "id" đầu tiên bên trong
            Match userMatch = Regex.Match(json, "\"user\"\\s*:\\s*\\{");
            if (userMatch.Success)
            {
                string sub = json.Substring(userMatch.Index + userMatch.Length);
                return ExtractJsonValue(sub, "id");
            }
            // Fallback: lấy "id" ở top level
            return ExtractJsonValue(json, "id");
        }

        /// <summary>
        /// Trích xuất giá trị string từ JSON bằng regex.
        /// </summary>
        internal static string ExtractJsonValue(string json, string key)
        {
            string pattern = String.Format(
                "\"{0}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"",
                Regex.Escape(key));
            Match match = Regex.Match(json, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\")
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r");
            }
            return null;
        }

        /// <summary>
        /// Trích xuất giá trị number từ JSON bằng regex.
        /// </summary>
        internal static string ExtractJsonNumber(string json, string key)
        {
            string pattern = String.Format(
                "\"{0}\"\\s*:\\s*([0-9]+)",
                Regex.Escape(key));
            Match match = Regex.Match(json, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        /// <summary>
        /// Escape ký tự đặc biệt cho JSON string value.
        /// </summary>
        internal static string EscapeJsonString(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}
