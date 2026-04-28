using System;
using System.Net;
using System.Text;

namespace VinTed.Licensing
{
    /// <summary>
    /// Client gọi Edge Functions: verify-license, create-order, order-status.
    /// </summary>
    public static class LicenseClient
    {
        /// <summary>
        /// Xác minh license với server.
        /// </summary>
        public static LicenseInfo VerifyLicense(string accessToken, string deviceId, string appVersion)
        {
            LicenseInfo info = new LicenseInfo();
            try
            {
                string url = LicenseConfig.SupabaseUrl + LicenseConfig.VerifyLicensePath;
                string body = String.Format(
                    "{{\"device_id\":\"{0}\",\"app_version\":\"{1}\",\"device_name\":\"{2}\"}}",
                    SupabaseAuth.EscapeJsonString(deviceId),
                    SupabaseAuth.EscapeJsonString(appVersion),
                    SupabaseAuth.EscapeJsonString(DeviceFingerprint.GetDeviceName()));

                string response = PostJsonWithAuth(url, body, accessToken);

                // Parse response
                info.Status = SupabaseAuth.ExtractJsonValue(response, "status") ?? "inactive";
                info.Plan = SupabaseAuth.ExtractJsonValue(response, "plan") ?? "";

                string expiresAtStr = SupabaseAuth.ExtractJsonValue(response, "expires_at");
                if (!string.IsNullOrEmpty(expiresAtStr))
                {
                    DateTime expiresAt;
                    if (DateTime.TryParse(expiresAtStr, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out expiresAt))
                    {
                        info.ExpiresAt = expiresAt;
                    }
                }

                string serverTimeStr = SupabaseAuth.ExtractJsonValue(response, "server_time");
                if (!string.IsNullOrEmpty(serverTimeStr))
                {
                    DateTime serverTime;
                    if (DateTime.TryParse(serverTimeStr, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out serverTime))
                    {
                        info.ServerTime = serverTime;
                    }
                }

                string maxDevicesStr = SupabaseAuth.ExtractJsonNumber(response, "max_devices");
                if (!string.IsNullOrEmpty(maxDevicesStr))
                {
                    int maxDevices;
                    if (int.TryParse(maxDevicesStr, out maxDevices))
                    {
                        info.MaxDevices = maxDevices;
                    }
                }

                // Lấy email và userId từ token store
                info.UserEmail = SecureTokenStore.GetEmail() ?? "";
                info.UserId = SecureTokenStore.GetUserId() ?? "";

                // Kiểm tra lỗi
                string error = SupabaseAuth.ExtractJsonValue(response, "error");
                if (!string.IsNullOrEmpty(error))
                {
                    info.Status = "inactive";
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse httpResponse = ex.Response as HttpWebResponse;
                if (httpResponse != null && httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Token hết hạn — thử refresh
                    info.Status = "token_expired";
                }
                else
                {
                    // Không có mạng — để LicenseManager dùng cache
                    info.Status = "offline";
                }
            }
            catch (Exception)
            {
                info.Status = "offline";
            }
            return info;
        }

        /// <summary>
        /// Tạo đơn hàng mới để mua license.
        /// </summary>
        public static OrderInfo CreateOrder(string accessToken, string planId)
        {
            OrderInfo order = new OrderInfo();
            try
            {
                string url = LicenseConfig.SupabaseUrl + LicenseConfig.CreateOrderPath;
                string body = String.Format(
                    "{{\"plan_id\":\"{0}\"}}",
                    SupabaseAuth.EscapeJsonString(planId));

                string response = PostJsonWithAuth(url, body, accessToken);

                order.OrderId = SupabaseAuth.ExtractJsonValue(response, "order_id") ?? "";
                order.OrderCode = SupabaseAuth.ExtractJsonValue(response, "order_code") ?? "";
                order.QrUrl = SupabaseAuth.ExtractJsonValue(response, "qr_url") ?? "";

                string amountStr = SupabaseAuth.ExtractJsonNumber(response, "amount_vnd");
                if (!string.IsNullOrEmpty(amountStr))
                {
                    int amount;
                    if (int.TryParse(amountStr, out amount))
                    {
                        order.AmountVnd = amount;
                    }
                }

                string expiresAtStr = SupabaseAuth.ExtractJsonValue(response, "expires_at");
                if (!string.IsNullOrEmpty(expiresAtStr))
                {
                    DateTime expiresAt;
                    if (DateTime.TryParse(expiresAtStr, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out expiresAt))
                    {
                        order.ExpiresAt = expiresAt;
                    }
                }

                // Kiểm tra lỗi
                string error = SupabaseAuth.ExtractJsonValue(response, "error");
                if (!string.IsNullOrEmpty(error))
                {
                    order.ErrorMessage = error;
                }
            }
            catch (WebException ex)
            {
                order.ErrorMessage = ParseWebError(ex);
            }
            catch (Exception ex)
            {
                order.ErrorMessage = ex.Message;
            }
            return order;
        }

        /// <summary>
        /// Kiểm tra trạng thái đơn hàng (polling).
        /// </summary>
        public static OrderStatusResult CheckOrderStatus(string accessToken, string orderId)
        {
            OrderStatusResult status = new OrderStatusResult();
            try
            {
                string url = String.Format("{0}{1}?order_id={2}",
                    LicenseConfig.SupabaseUrl,
                    LicenseConfig.OrderStatusPath,
                    Uri.EscapeDataString(orderId));

                string response = GetWithAuth(url, accessToken);

                status.Status = SupabaseAuth.ExtractJsonValue(response, "status") ?? "pending";

                string licenseExpiresStr = SupabaseAuth.ExtractJsonValue(response, "license_expires_at");
                if (!string.IsNullOrEmpty(licenseExpiresStr))
                {
                    DateTime licenseExpires;
                    if (DateTime.TryParse(licenseExpiresStr, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out licenseExpires))
                    {
                        status.LicenseExpiresAt = licenseExpires;
                    }
                }
            }
            catch (Exception)
            {
                status.Status = "error";
            }
            return status;
        }

        // ===== HTTP Helpers =====

        private static string PostJsonWithAuth(string url, string jsonBody, string accessToken)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json");
                client.Headers.Add("apikey", LicenseConfig.SupabaseAnonKey);
                client.Headers.Add("Authorization", "Bearer " + accessToken);
                client.Encoding = Encoding.UTF8;
                return client.UploadString(url, "POST", jsonBody);
            }
        }

        private static string GetWithAuth(string url, string accessToken)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("apikey", LicenseConfig.SupabaseAnonKey);
                client.Headers.Add("Authorization", "Bearer " + accessToken);
                client.Encoding = Encoding.UTF8;
                return client.DownloadString(url);
            }
        }

        private static string ParseWebError(WebException ex)
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
                                string msg = SupabaseAuth.ExtractJsonValue(body, "error");
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
    }
}
