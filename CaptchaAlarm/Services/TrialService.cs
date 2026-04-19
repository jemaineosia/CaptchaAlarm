using Microsoft.Win32;
using System.Management;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CaptchaAlarm.Services
{
    /// <summary>
    /// Manages per-tester licensing via jsonbin.io.
    /// The developer explicitly activates each tester's HWID with a set duration.
    /// The app never self-registers — it is blocked until the developer adds the license.
    ///
    /// jsonbin.io structure:
    /// {
    ///   "HWID1": { "activated": "2026-04-17T00:00:00Z", "days": 30 },
    ///   "HWID2": { "activated": "2026-04-17T00:00:00Z", "days": 7  }
    /// }
    /// </summary>
    internal static class TrialService
    {
        // ?? jsonbin.io config ?????????????????????????????????????????????????
        // Update these with your jsonbin.io credentials.
        // Use LicenseManager.ps1 to manage tester licenses.
        internal const string JsonBinApiKey  = "$2a$10$x788QPCES.yVltsT/8XGte8kcfKt7hFYJvNssAHCaxFYZLguEqAea";
        internal const string JsonBinBinId   = "69dc8e49856a68218929fc75";
        internal const string JsonBinBaseUrl = "https://api.jsonbin.io/v3/b/";

        // Registry — mirrors remote record for offline validation
        private const string RegPath     = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost\Settings";
        private const string RegValDate  = "CacheToken";
        private const string RegValDays  = "CacheDuration";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

        // ?? Public API ????????????????????????????????????????????????????????

        public enum TrialStatus { Valid, Expired, NotActivated, Error }

        private record LicenseRecord(DateTime ActivationDate, int DurationDays);

        /// <summary>
        /// Checks the license status for this machine.
        /// Remote (jsonbin.io) is authoritative; local registry is the offline fallback.
        /// If the HWID is not found in either, returns <see cref="TrialStatus.NotActivated"/>.
        /// </summary>
        public static async Task<(TrialStatus Status, int DaysLeft, string Message)> CheckAsync()
        {
            try
            {
                string hwid = GetHwid();
                AppLogger.Log($"[License] HWID: {hwid}");

                var record = await FetchRemoteRecordAsync(hwid);

                if (record != null)
                {
                    // Sync to local registry for offline use
                    SaveLocal(record.ActivationDate, record.DurationDays);

                    int daysUsed = (int)(DateTime.UtcNow - record.ActivationDate).TotalDays;
                    int daysLeft = record.DurationDays - daysUsed;

                    AppLogger.Log($"[License] Activated: {record.ActivationDate:yyyy-MM-dd}, " +
                                  $"Duration: {record.DurationDays}d, Left: {daysLeft}d");

                    if (daysLeft > 0)
                        return (TrialStatus.Valid, daysLeft, $"License active — {daysLeft} day(s) remaining.");

                    return (TrialStatus.Expired, 0, "Your license has expired.");
                }

                // HWID not in remote — check local cache (covers offline scenario)
                return FallbackLocalCheck();
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[License] Remote check failed: {ex.Message}");
                return FallbackLocalCheck();
            }
        }

        /// <summary>Public wrapper so the UI can display the HWID.</summary>
        public static string GetHwidPublic() => GetHwid();

        // ?? HWID ??????????????????????????????????????????????????????????????

        private static string GetHwid()
        {
            var parts = new List<string>();

            try
            {
                using var cpu = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (ManagementObject obj in cpu.Get())
                    parts.Add(obj["ProcessorId"]?.ToString() ?? string.Empty);
            }
            catch { /* ignore */ }

            try
            {
                using var mb = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (ManagementObject obj in mb.Get())
                    parts.Add(obj["SerialNumber"]?.ToString() ?? string.Empty);
            }
            catch { /* ignore */ }

            string raw = string.Join("|", parts);
            if (string.IsNullOrWhiteSpace(raw)) raw = Environment.MachineName;

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash)[..16];
        }

        // ?? jsonbin.io ????????????????????????????????????????????????????????

        private static async Task<LicenseRecord?> FetchRemoteRecordAsync(string hwid)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, JsonBinBaseUrl + JsonBinBinId);
            req.Headers.Add("X-Master-Key", JsonBinApiKey);
            req.Headers.Add("X-Bin-Meta", "false");

            var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            string json = await resp.Content.ReadAsStringAsync();
            var bin = JObject.Parse(json);

            var entry = bin[hwid];
            if (entry == null) return null;

            // Support legacy format: "HWID": "date-string"  (treated as 7-day)
            if (entry.Type == JTokenType.String)
            {
                if (DateTime.TryParse(entry.ToString(), out DateTime legacyDate))
                    return new LicenseRecord(DateTime.SpecifyKind(legacyDate, DateTimeKind.Utc), 7);
                return null;
            }

            // New format: "HWID": { "activated": "...", "days": N }
            string? dateStr = entry["activated"]?.ToString();
            int days = entry["days"]?.Value<int>() ?? 7;

            if (string.IsNullOrEmpty(dateStr) || !DateTime.TryParse(dateStr, out DateTime activation))
                return null;

            return new LicenseRecord(DateTime.SpecifyKind(activation, DateTimeKind.Utc), days);
        }

        // ?? Registry ??????????????????????????????????????????????????????????

        private static void SaveLocal(DateTime date, int days)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegPath, writable: true);
                key?.SetValue(RegValDate, Obfuscate(date.ToString("o")), RegistryValueKind.String);
                key?.SetValue(RegValDays, Obfuscate(days.ToString()), RegistryValueKind.String);
            }
            catch (Exception ex) { AppLogger.Log($"[License] Registry save failed: {ex.Message}"); }
        }

        private static (DateTime? Date, int Days) LoadLocal()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegPath);
                if (key == null) return (null, 0);

                DateTime? date = null;
                int days = 0;

                if (key.GetValue(RegValDate) is string encDate)
                {
                    string decoded = Deobfuscate(encDate);
                    if (DateTime.TryParse(decoded, out DateTime d))
                        date = DateTime.SpecifyKind(d, DateTimeKind.Utc);
                }

                if (key.GetValue(RegValDays) is string encDays)
                {
                    string decoded = Deobfuscate(encDays);
                    if (int.TryParse(decoded, out int n))
                        days = n;
                }

                return (date, days);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[License] Registry read failed: {ex.Message}");
                return (null, 0);
            }
        }

        // ?? Fallback (offline / not activated) ???????????????????????????????

        private static (TrialStatus, int, string) FallbackLocalCheck()
        {
            var (date, days) = LoadLocal();

            if (date == null || days <= 0)
            {
                AppLogger.Log("[License] No local record — not activated.");
                return (TrialStatus.NotActivated, 0, "License not activated.");
            }

            int daysUsed = (int)(DateTime.UtcNow - date.Value).TotalDays;
            int daysLeft = days - daysUsed;

            AppLogger.Log($"[License] Offline fallback — {daysLeft} day(s) left.");

            if (daysLeft > 0)
                return (TrialStatus.Valid, daysLeft, $"License active (offline) — {daysLeft} day(s) remaining.");

            return (TrialStatus.Expired, 0, "Your license has expired.");
        }

        // ?? XOR obfuscation ???????????????????????????????????????????????????

        private static string Obfuscate(string input)
        {
            const string key = "CA7x!mK2";
            var sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
                sb.Append((char)(input[i] ^ key[i % key.Length]));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        private static string Deobfuscate(string input)
        {
            const string key = "CA7x!mK2";
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(input));
            var sb = new StringBuilder(decoded.Length);
            for (int i = 0; i < decoded.Length; i++)
                sb.Append((char)(decoded[i] ^ key[i % key.Length]));
            return sb.ToString();
        }
    }
}

