using System;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace VinTed.Licensing
{
    /// <summary>
    /// Tạo Device Fingerprint duy nhất cho mỗi máy tính.
    /// Hash = SHA256(MachineGuid + WindowsSID).
    /// Không gửi raw hardware ID lên server.
    /// </summary>
    public static class DeviceFingerprint
    {
        private static string _cachedHash;

        /// <summary>
        /// Lấy device hash (SHA256). Kết quả được cache trong memory.
        /// </summary>
        public static string GetDeviceHash()
        {
            if (!string.IsNullOrEmpty(_cachedHash))
            {
                return _cachedHash;
            }

            try
            {
                string machineGuid = GetMachineGuid();
                string windowsSid = GetWindowsSid();
                string raw = machineGuid + "|" + windowsSid;

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    _cachedHash = sb.ToString();
                }
            }
            catch (Exception)
            {
                // Fallback: dùng machine name + username
                string fallback = Environment.MachineName + "|" + Environment.UserName;
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fallback));
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    _cachedHash = sb.ToString();
                }
            }

            return _cachedHash;
        }

        /// <summary>
        /// Lấy tên máy tính để hiển thị trên UI.
        /// </summary>
        public static string GetDeviceName()
        {
            return Environment.MachineName;
        }

        /// <summary>
        /// Đọc MachineGuid từ Registry.
        /// Giá trị này unique cho mỗi cài đặt Windows.
        /// </summary>
        private static string GetMachineGuid()
        {
            string guid = "";
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Cryptography", false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("MachineGuid");
                        if (val != null)
                        {
                            guid = val.ToString();
                        }
                    }
                }
            }
            catch (Exception)
            {
                guid = "unknown-machine";
            }
            return guid;
        }

        /// <summary>
        /// Lấy Windows SID của user hiện tại.
        /// </summary>
        private static string GetWindowsSid()
        {
            string sid = "";
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                if (identity != null && identity.User != null)
                {
                    sid = identity.User.Value;
                }
            }
            catch (Exception)
            {
                sid = "unknown-sid";
            }
            return sid;
        }
    }
}
