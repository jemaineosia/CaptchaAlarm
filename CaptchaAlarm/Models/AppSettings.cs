using Newtonsoft.Json;

namespace CaptchaAlarm.Models
{
    /// <summary>
    /// Application settings model. Persisted to appsettings.json.
    /// </summary>
    public class AppSettings
    {
        /// <summary>Screen capture interval in milliseconds (1000–10000).</summary>
        [JsonProperty("MonitoringInterval")]
        public int MonitoringInterval { get; set; } = 2000;

        /// <summary>Path to the WAV/MP3 alarm sound file.</summary>
        [JsonProperty("AlarmSoundPath")]
        public string AlarmSoundPath { get; set; } = "Resources/default-alarm.wav";

        /// <summary>Whether Windows toast notifications are enabled.</summary>
        [JsonProperty("EnableNotifications")]
        public bool EnableNotifications { get; set; } = true;

        /// <summary>Whether the alarm sound is enabled.</summary>
        [JsonProperty("EnableSound")]
        public bool EnableSound { get; set; } = true;

        /// <summary>Whether to loop the alarm sound.</summary>
        [JsonProperty("LoopSound")]
        public bool LoopSound { get; set; } = true;

        /// <summary>Number of times to loop the alarm sound (1–10).</summary>
        [JsonProperty("LoopCount")]
        public int LoopCount { get; set; } = 3;

        /// <summary>Confidence score threshold (0–100) to trigger the alarm.</summary>
        [JsonProperty("ConfidenceThreshold")]
        public int ConfidenceThreshold { get; set; } = 70;

        /// <summary>Whether to save a screenshot on each CAPTCHA detection.</summary>
        [JsonProperty("SaveScreenshots")]
        public bool SaveScreenshots { get; set; } = false;

        /// <summary>Directory path where detection screenshots are saved.</summary>
        [JsonProperty("ScreenshotPath")]
        public string ScreenshotPath { get; set; } = "Screenshots";

        /// <summary>Whether the application minimizes to the system tray.</summary>
        [JsonProperty("MinimizeToTray")]
        public bool MinimizeToTray { get; set; } = true;
    }
}
