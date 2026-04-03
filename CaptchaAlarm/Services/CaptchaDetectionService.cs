using System.Drawing;
using System.Drawing.Imaging;
using CaptchaAlarm.Models;
using Tesseract;

namespace CaptchaAlarm.Services
{
    /// <summary>
    /// Analyses screenshots for the Aion Daeva Verification CAPTCHA using a
    /// multi-layered detection approach (OCR + color + layout).
    /// </summary>
    public class CaptchaDetectionService : IDisposable
    {
        // ── OCR keywords (case-insensitive) ────────────────────────────────────
        private static readonly string[] OcrKeywords =
        {
            "daeva verification",
            "enter the text you see",
            "chance(s) remaining",
            "chances remaining",
            "failing to complete verification",
            "debuffs",
            "confirm"
        };

        // Points per keyword match
        private const int OcrPointsPerKeyword = 50;

        // ── Color thresholds ────────────────────────────────────────────────────
        // Dark background (≈ #2d2d2d ± 15)
        private const int DarkMin = 30, DarkMax = 60;
        // Yellow/orange timer pixels
        private const int YellowRMin = 180, YellowGMin = 120, YellowBMax = 80;
        // Cyan/blue "chances remaining" text
        private const int CyanRMax = 120, CyanGMin = 180, CyanBMin = 180;

        private const int ColorScoreMax = 30;
        private const int DarkBgScore = 20;
        private const int LayoutScore = 20;

        // How many dark-bg pixels must be sampled before counting the region
        private const int DarkPixelMinCount = 500;

        private TesseractEngine? _ocr;
        private bool _ocrAvailable;
        private bool _disposed;

        public CaptchaDetectionService()
        {
            TryInitOcr();
        }

        private void TryInitOcr()
        {
            // Common locations for tessdata
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                             "Tesseract-OCR", "tessdata"),
                @"C:\Program Files\Tesseract-OCR\tessdata",
                @"C:\tessdata"
            };

            foreach (var dir in candidates)
            {
                if (!Directory.Exists(dir)) continue;
                try
                {
                    _ocr = new TesseractEngine(dir, "eng", EngineMode.LstmOnly);
                    _ocr.SetVariable("tessedit_char_whitelist", "");
                    _ocrAvailable = true;
                    AppLogger.Log($"[Detection] Tesseract OCR initialised from: {dir}");
                    return;
                }
                catch (Exception ex)
                {
                    AppLogger.Log($"[Detection] OCR init failed at {dir}: {ex.Message}");
                }
            }

            AppLogger.Log("[Detection] Tesseract OCR not available – colour/layout detection only.");
            _ocrAvailable = false;
        }

        /// <summary>
        /// Analyses the supplied bitmap and returns a <see cref="DetectionResult"/>.
        /// The caller retains ownership of the bitmap.
        /// </summary>
        public DetectionResult Analyse(Bitmap screenshot, int confidenceThreshold)
        {
            var result = new DetectionResult { Timestamp = DateTime.UtcNow };

            // ── Layer 1 : OCR ──────────────────────────────────────────────────
            if (_ocrAvailable && _ocr != null)
            {
                result.OcrText = RunOcr(screenshot);
                if (result.OcrText != null)
                {
                    var lower = result.OcrText.ToLowerInvariant();
                    foreach (var kw in OcrKeywords)
                    {
                        if (lower.Contains(kw, StringComparison.OrdinalIgnoreCase))
                        {
                            result.MatchedKeywords.Add(kw);
                            result.OcrScore += OcrPointsPerKeyword;
                        }
                    }
                }
            }

            // ── Layer 2 : Colour signatures ────────────────────────────────────
            result.ColorScore = AnalyseColors(screenshot);

            // ── Layer 3 : Layout (dark rectangle) ─────────────────────────────
            result.LayoutScore = AnalyseLayout(screenshot);

            // ── Aggregate ─────────────────────────────────────────────────────
            result.ConfidenceScore = result.OcrScore + result.ColorScore + result.LayoutScore;
            result.IsDetected = result.ConfidenceScore >= confidenceThreshold;

            return result;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private string? RunOcr(Bitmap bmp)
        {
            try
            {
                using var pix = BitmapToPix(bmp);
                if (pix == null) return null;

                using var page = _ocr!.Process(pix);
                return page.GetText();
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Detection] OCR processing error: {ex.Message}");
                return null;
            }
        }

        /// <summary>Converts a System.Drawing.Bitmap to a Tesseract Pix.</summary>
        private static Pix? BitmapToPix(Bitmap bmp)
        {
            try
            {
                using var ms = new System.IO.MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                return Pix.LoadFromMemory(ms.ToArray());
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Detection] BitmapToPix error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Scans a uniform sample of pixels and awards colour-signature points.
        /// </summary>
        private static int AnalyseColors(Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            int step = Math.Max(4, width / 200); // sample ~200 columns

            int darkCount = 0, yellowCount = 0, cyanCount = 0;

            for (int x = 0; x < width; x += step)
            {
                for (int y = 0; y < height; y += step)
                {
                    Color c = bmp.GetPixel(x, y);

                    if (c.R is >= DarkMin and <= DarkMax &&
                        c.G is >= DarkMin and <= DarkMax &&
                        c.B is >= DarkMin and <= DarkMax)
                        darkCount++;

                    if (c.R >= YellowRMin && c.G >= YellowGMin && c.B <= YellowBMax)
                        yellowCount++;

                    if (c.R <= CyanRMax && c.G >= CyanGMin && c.B >= CyanBMin)
                        cyanCount++;
                }
            }

            int score = 0;
            if (darkCount >= DarkPixelMinCount) score += DarkBgScore;
            if (yellowCount >= 10) score += 15;
            if (cyanCount >= 10) score += 15;

            return Math.Min(score, ColorScoreMax + DarkBgScore);
        }

        /// <summary>
        /// Awards layout score when a large dark rectangle is detected near the
        /// screen centre (typical position of the Aion verification popup).
        /// </summary>
        private static int AnalyseLayout(Bitmap bmp)
        {
            int cx = bmp.Width / 2;
            int cy = bmp.Height / 2;

            // Sample a band around screen centre
            int darkInCenter = 0;
            int sampleW = bmp.Width / 4;
            int sampleH = bmp.Height / 4;
            int step = 8;

            for (int x = cx - sampleW; x < cx + sampleW; x += step)
            {
                for (int y = cy - sampleH; y < cy + sampleH; y += step)
                {
                    if (x < 0 || y < 0 || x >= bmp.Width || y >= bmp.Height) continue;
                    Color c = bmp.GetPixel(x, y);
                    if (c.R is >= DarkMin and <= DarkMax &&
                        c.G is >= DarkMin and <= DarkMax &&
                        c.B is >= DarkMin and <= DarkMax)
                        darkInCenter++;
                }
            }

            return darkInCenter >= 200 ? LayoutScore : 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _ocr?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
