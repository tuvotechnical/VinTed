using System;

namespace VinTed.Licensing
{
    /// <summary>
    /// Cấu hình kết nối Supabase và endpoints.
    /// CHỈ chứa public keys — an toàn để commit lên GitHub.
    /// KHÔNG BAO GIỜ đặt service_role key hoặc SePay API key ở đây.
    /// </summary>
    public static class LicenseConfig
    {
        public const string SupabaseUrl = "https://xjbpsucnldzktkahauix.supabase.co";
        public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InhqYnBzdWNubGR6a3RrYWhhdWl4Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwOTA2MTksImV4cCI6MjA5MjY2NjYxOX0.2cVTGzNMkFWUuRN6F538KSTntCDFqP32nPHj4daGzaw";

        // Supabase Auth endpoints
        public const string AuthSignUpPath = "/auth/v1/signup";
        public const string AuthSignInPath = "/auth/v1/token?grant_type=password";
        public const string AuthRefreshPath = "/auth/v1/token?grant_type=refresh_token";

        // Edge Function endpoints
        public const string VerifyLicensePath = "/functions/v1/verify-license";
        public const string CreateOrderPath = "/functions/v1/create-order";
        public const string OrderStatusPath = "/functions/v1/order-status";

        // Offline grace periods (giờ)
        public const int DailyGraceHours = 24;
        public const int MonthlyGraceHours = 72;
        public const int YearlyGraceHours = 168; // 7 ngày

        // Order polling interval (ms)
        public const int OrderPollIntervalMs = 5000;

        // Order timeout (phút)
        public const int OrderTimeoutMinutes = 30;

        // Thư mục lưu dữ liệu license
        public static string GetDataFolder()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = System.IO.Path.Combine(appData, "VinTed");
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            return folder;
        }
    }
}
