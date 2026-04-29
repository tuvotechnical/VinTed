using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace VinTed.Updater
{
    /// <summary>
    /// Kết quả kiểm tra update từ GitHub Releases.
    /// </summary>
    public class UpdateCheckResult
    {
        private bool _hasUpdate;
        private string _latestVersion;
        private string _currentVersion;
        private string _downloadUrl;
        private string _releaseNotes;
        private string _htmlUrl;

        public bool HasUpdate
        {
            get { return _hasUpdate; }
            set { _hasUpdate = value; }
        }

        public string LatestVersion
        {
            get { return _latestVersion; }
            set { _latestVersion = value; }
        }

        public string CurrentVersion
        {
            get { return _currentVersion; }
            set { _currentVersion = value; }
        }

        public string DownloadUrl
        {
            get { return _downloadUrl; }
            set { _downloadUrl = value; }
        }

        public string ReleaseNotes
        {
            get { return _releaseNotes; }
            set { _releaseNotes = value; }
        }

        public string HtmlUrl
        {
            get { return _htmlUrl; }
            set { _htmlUrl = value; }
        }
    }

    /// <summary>
    /// Kiểm tra phiên bản mới trên GitHub Releases.
    /// Dùng GitHub REST API (không cần authentication cho public repo).
    /// </summary>
    public static class UpdateChecker
    {
        private const string GitHubApiUrl =
            "https://api.github.com/repos/tuvotechnical/VinTed/releases/latest";

        /// <summary>
        /// Đường dẫn file lưu version đã bỏ qua (nằm cạnh VinTed.dll).
        /// </summary>
        private static string GetSkipFilePath()
        {
            string folder = System.IO.Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
            return System.IO.Path.Combine(folder, "update_skip.txt");
        }

        /// <summary>
        /// Kiểm tra xem user đã bỏ qua version này chưa.
        /// </summary>
        public static bool IsVersionSkipped(string version)
        {
            try
            {
                string skipFile = GetSkipFilePath();
                if (System.IO.File.Exists(skipFile))
                {
                    string skipped = System.IO.File.ReadAllText(skipFile).Trim();
                    return string.Equals(skipped, version, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception) { }
            return false;
        }

        /// <summary>
        /// Lưu version đã bỏ qua.
        /// </summary>
        public static void SkipVersion(string version)
        {
            try
            {
                string skipFile = GetSkipFilePath();
                System.IO.File.WriteAllText(skipFile, version);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Lấy version hiện tại từ Assembly.
        /// </summary>
        public static string GetCurrentVersion()
        {
            Version ver = Assembly.GetExecutingAssembly().GetName().Version;
            return NormalizeVersion(String.Format("{0}.{1}.{2}", ver.Major, ver.Minor, ver.Build));
        }

        /// <summary>
        /// Kiểm tra update từ GitHub Releases API.
        /// Trả về UpdateCheckResult.
        /// Method này BLOCKING — gọi từ background thread.
        /// </summary>
        public static UpdateCheckResult CheckForUpdate()
        {
            UpdateCheckResult result = new UpdateCheckResult();
            result.HasUpdate = false;
            result.CurrentVersion = GetCurrentVersion();

            try
            {
                // Tạo request đến GitHub API
                using (WebClient client = new WebClient())
                {
                    // GitHub API yêu cầu User-Agent
                    client.Headers.Add("User-Agent", "VinTed-Updater");
                    client.Headers.Add("Accept", "application/vnd.github.v3+json");
                    client.Encoding = System.Text.Encoding.UTF8;

                    string json = client.DownloadString(GitHubApiUrl);

                    // Parse tag_name (version)
                    string tagName = ExtractJsonValue(json, "tag_name");
                    if (string.IsNullOrEmpty(tagName))
                    {
                        return result;
                    }

                    string latestVersion = NormalizeVersion(tagName);
                    if (string.IsNullOrEmpty(latestVersion))
                    {
                        return result;
                    }
                    result.LatestVersion = latestVersion;

                    // Parse release notes
                    result.ReleaseNotes = ExtractJsonValue(json, "body");
                    if (result.ReleaseNotes != null)
                    {
                        // Unescape JSON string
                        result.ReleaseNotes = result.ReleaseNotes
                            .Replace("\\n", "\n")
                            .Replace("\\r", "\r")
                            .Replace("\\t", "\t")
                            .Replace("\\\"", "\"");
                    }

                    // Parse html_url (link đến trang release)
                    result.HtmlUrl = ExtractJsonValue(json, "html_url");

                    // Parse download URL từ assets
                    result.DownloadUrl = ExtractFirstAssetUrl(json);
                    if (string.IsNullOrEmpty(result.DownloadUrl))
                    {
                        // Fallback: dùng html_url
                        result.DownloadUrl = result.HtmlUrl;
                    }

                    // So sánh version
                    result.HasUpdate = IsNewerVersion(result.CurrentVersion, latestVersion);
                }
            }
            catch (WebException)
            {
                // Không có internet hoặc API lỗi — bỏ qua im lặng
            }
            catch (Exception)
            {
                // Bỏ qua mọi lỗi — update check không được làm crash Inventor
            }

            if (string.IsNullOrEmpty(result.LatestVersion))
            {
                result.LatestVersion = result.CurrentVersion;
            }

            return result;
        }

        /// <summary>
        /// Chuẩn hóa chuỗi version/tag về SemVer 3 phần số x.y.z.
        /// Ví dụ: v1.1.5, 1.1.5.0, release-1.1.5-hotfix đều trả về 1.1.5.
        /// </summary>
        private static string NormalizeVersion(string versionText)
        {
            if (string.IsNullOrEmpty(versionText))
            {
                return null;
            }

            Match match = Regex.Match(versionText, "(\\d+)\\.(\\d+)\\.(\\d+)");
            if (!match.Success)
            {
                return null;
            }

            int major = 0;
            int minor = 0;
            int patch = 0;
            if (!int.TryParse(match.Groups[1].Value, out major)) return null;
            if (!int.TryParse(match.Groups[2].Value, out minor)) return null;
            if (!int.TryParse(match.Groups[3].Value, out patch)) return null;

            return String.Format("{0}.{1}.{2}", major, minor, patch);
        }

        /// <summary>
        /// So sánh 2 version string (x.y.z). Trả về true nếu latest > current.
        /// </summary>
        private static bool IsNewerVersion(string current, string latest)
        {
            try
            {
                string normalizedCurrent = NormalizeVersion(current);
                string normalizedLatest = NormalizeVersion(latest);
                if (string.IsNullOrEmpty(normalizedCurrent) || string.IsNullOrEmpty(normalizedLatest))
                {
                    return false;
                }

                Version currentVersion = new Version(normalizedCurrent);
                Version latestVersion = new Version(normalizedLatest);
                return latestVersion.CompareTo(currentVersion) > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Trích xuất giá trị string từ JSON bằng regex (tránh thêm dependency JSON).
        /// Chỉ hoạt động với key ở top-level hoặc nested đơn giản.
        /// </summary>
        private static string ExtractJsonValue(string json, string key)
        {
            // Pattern: "key": "value" hoặc "key":"value"
            string pattern = String.Format("\"{0}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", Regex.Escape(key));
            Match match = Regex.Match(json, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Thử match null value
            string nullPattern = String.Format("\"{0}\"\\s*:\\s*null", Regex.Escape(key));
            if (Regex.IsMatch(json, nullPattern))
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Trích xuất URL download của asset đầu tiên (browser_download_url).
        /// </summary>
        private static string ExtractFirstAssetUrl(string json)
        {
            string pattern = "\"browser_download_url\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"";
            Match match = Regex.Match(json, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }
    }
}
