namespace CaptchaAlarm.Models
{
    /// <summary>
    /// A single entry in the UI detection log.
    /// </summary>
    public class DetectionLog
    {
        /// <summary>Local time of the detection event.</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>Total confidence score that triggered this log entry.</summary>
        public int ConfidenceScore { get; set; }

        /// <summary>Short description / summary of the detection.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Keywords matched during OCR analysis.</summary>
        public List<string> MatchedKeywords { get; set; } = new();

        /// <summary>Formatted display string for the ListBox.</summary>
        public override string ToString() =>
            $"[{Timestamp:HH:mm:ss}] CAPTCHA Detected! Score: {ConfidenceScore} | {Message}";
    }
}
