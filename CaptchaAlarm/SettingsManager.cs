using CaptchaAlarm.Models;
using Newtonsoft.Json;

namespace CaptchaAlarm
{
    /// <summary>
    /// Loads and persists <see cref="AppSettings"/> to/from <c>appsettings.json</c>.
    /// </summary>
    public static class SettingsManager
    {
        private static readonly string SettingsPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        /// <summary>Loads settings from disk, or returns defaults if not found.</summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Settings] Load error: {ex.Message}");
            }

            return new AppSettings();
        }

        /// <summary>Saves settings to disk.</summary>
        public static void Save(AppSettings settings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Settings] Save error: {ex.Message}");
            }
        }
    }
}
