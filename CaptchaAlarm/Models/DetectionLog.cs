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

        /// <summary>Whether this entry triggered the alarm (score ≥ 200).</summary>
        public bool AlarmTriggered { get; set; }

        /// <summary>Formatted display string for the ListBox.</summary>
        public override string ToString()
        {
            string prefix = AlarmTriggered ? "⚠ ALARM" : "· Logged";
            return $"[{Timestamp:HH:mm:ss}] {prefix} | Score: {ConfidenceScore} | {Message}";
        }
    }
}
