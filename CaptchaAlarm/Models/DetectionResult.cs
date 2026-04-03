namespace CaptchaAlarm.Models
{
    /// <summary>
    /// Represents the result of a single CAPTCHA detection analysis.
    /// </summary>
    public class DetectionResult
    {
        /// <summary>UTC timestamp of the detection.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Total confidence score (0–100+).</summary>
        public int ConfidenceScore { get; set; }

        /// <summary>Whether the confidence score meets the configured threshold.</summary>
        public bool IsDetected { get; set; }

        /// <summary>List of OCR keywords that were matched.</summary>
        public List<string> MatchedKeywords { get; set; } = new();

        /// <summary>Points contributed by OCR keyword matching.</summary>
        public int OcrScore { get; set; }

        /// <summary>Points contributed by color signature detection.</summary>
        public int ColorScore { get; set; }

        /// <summary>Points contributed by visual layout pattern detection.</summary>
        public int LayoutScore { get; set; }

        /// <summary>Optional path to the saved screenshot file.</summary>
        public string? ScreenshotPath { get; set; }

        /// <summary>Raw OCR text extracted from the screenshot.</summary>
        public string? OcrText { get; set; }
    }
}
