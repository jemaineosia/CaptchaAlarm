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
            "enter text within the time limit",
            "chance(s) remaining",
            "chances remaining",
            "failing to complete verification",
            "debuffs",
            "sanctions",
            "confirm"
        };

        // Points per keyword match
        private const int OcrPointsPerKeyword = 50;

        // ── Color thresholds ────────────────────────────────────────────────────
        // Dark popup background – widened to cover dark blue-gray (#141619 – #555555)
        private const int DarkMin = 15, DarkMax = 85;
        // Yellow/orange timer pixels
        private const int YellowRMin = 190, YellowGMin = 130, YellowBMax = 80;
        // Cyan/blue "chances remaining" text
        private const int CyanRMax = 120, CyanGMin = 180, CyanBMin = 180;

        private const int ColorScoreMax = 50;
        private const int DarkBgScore = 20;
        private const int LayoutScore = 20;
        private const int CaptchaImageScore = 30;

        // Lowered: popup has white CAPTCHA widget taking up part of its area
        private const int DarkPixelMinCount = 500;

        private const int YellowMinCount = 15;
        private const int CyanMinCount = 10;
        private const int CaptchaImageMinPixels = 20;

        private TesseractEngine? _ocr;
        private bool _ocrAvailable;
        private bool _disposed;

        /// <summary>True when Tesseract initialised successfully and OCR scoring is active.</summary>
        public bool IsOcrAvailable => _ocrAvailable;

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

            // ── Layer 4 : Colorful CAPTCHA image widget ────────────────────────
            result.ColorScore += AnalyseCaptchaImage(screenshot);

            // ── Layer 5 : Extract CAPTCHA region for 2captcha ─────────────────
            result.CaptchaRegion = ExtractCaptchaRegion(screenshot);

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
            if (yellowCount >= YellowMinCount) score += 15;
            if (cyanCount >= CyanMinCount) score += 15;

            return Math.Min(score, ColorScoreMax + DarkBgScore);
        }

        /// <summary>
        /// Awards layout score when a large dark rectangle is detected anywhere within
        /// the central two-thirds of the screen (popup can appear off-centre).
        /// </summary>
        private static int AnalyseLayout(Bitmap bmp)
        {
            int cx = bmp.Width / 2;
            int cy = bmp.Height / 2;

            // Wider sample – captcha window can appear off-screen-center
            int sampleW = bmp.Width / 3;
            int sampleH = bmp.Height / 3;
            int step = 8;

            int darkInCenter = 0;

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

            // Threshold scaled up proportionally for the wider sample area
            return darkInCenter >= 300 ? LayoutScore : 0;
        }

        /// <summary>
        /// Awards score when a colorful high-chroma image on a bright background is detected –
        /// the distinctive CAPTCHA widget with multicolored squiggle lines on white.
        /// </summary>
        private static int AnalyseCaptchaImage(Bitmap bmp)
        {
            int step = Math.Max(4, bmp.Width / 200);
            int colorfulCount = 0;

            for (int x = 0; x < bmp.Width; x += step)
            {
                for (int y = 0; y < bmp.Height; y += step)
                {
                    Color c = bmp.GetPixel(x, y);
                    int maxC = Math.Max(c.R, Math.Max(c.G, c.B));
                    int minC = Math.Min(c.R, Math.Min(c.G, c.B));
                    int brightness = (c.R + c.G + c.B) / 3;

                    // Bright pixel (light background) with high color variation (colorful line)
                    if (brightness > 150 && (maxC - minC) > 90)
                        colorfulCount++;
                }
            }

            return colorfulCount >= CaptchaImageMinPixels ? CaptchaImageScore : 0;
        }

        /// <summary>
        /// Locates the white CAPTCHA puzzle image box inside the dark popup and
        /// returns a cropped bitmap of just that widget.
        ///
        /// Strategy:
        ///   1. Find the bounding box of the dark popup panel (R,G,B all &lt; 90).
        ///   2. Within that region, find the largest contiguous white/near-white
        ///      rectangle (R,G,B all &gt; 220) – that is the puzzle image area.
        ///   3. Crop and return it. Returns null if not found.
        /// </summary>
        private static Bitmap? ExtractCaptchaRegion(Bitmap bmp)
        {
            int W = bmp.Width, H = bmp.Height;

            // ── Step 1: find dark popup bounding box ──────────────────────────
            int popLeft = W, popTop = H, popRight = 0, popBottom = 0;
            int step = 4;

            for (int x = 0; x < W; x += step)
            {
                for (int y = 0; y < H; y += step)
                {
                    Color c = bmp.GetPixel(x, y);
                    if (c.R < 90 && c.G < 90 && c.B < 90)
                    {
                        if (x < popLeft)   popLeft   = x;
                        if (y < popTop)    popTop    = y;
                        if (x > popRight)  popRight  = x;
                        if (y > popBottom) popBottom = y;
                    }
                }
            }

            if (popRight - popLeft < 100 || popBottom - popTop < 80)
                return null; // no popup found

            // ── Step 2: scan inside popup for white image box ─────────────────
            // Walk row by row; track the largest horizontal run of white pixels
            // to find the top/bottom/left/right of the white widget.
            int wLeft = popRight, wTop = popBottom, wRight = popLeft, wBottom = popTop;
            int whiteRowCount = 0;
            int bestWhiteRows = 0;
            int bestTop = -1, bestBottom = -1;

            for (int y = popTop; y <= popBottom; y += 2)
            {
                int runLeft = -1, runRight = -1, runLen = 0;

                for (int x = popLeft; x <= popRight; x += 2)
                {
                    Color c = bmp.GetPixel(Math.Min(x, W - 1), Math.Min(y, H - 1));
                    bool isWhite = c.R > 220 && c.G > 220 && c.B > 220;

                    if (isWhite)
                    {
                        if (runLeft < 0) runLeft = x;
                        runRight = x;
                        runLen++;
                    }
                }

                // A valid CAPTCHA image row has a wide white run (> 60px)
                if (runLen * 2 > 60)
                {
                    whiteRowCount++;
                    if (runLeft  < wLeft)   wLeft   = runLeft;
                    if (runRight > wRight)  wRight  = runRight;
                    if (y        < wTop)    wTop    = y;
                    if (y        > wBottom) wBottom = y;
                }
                else
                {
                    whiteRowCount = 0;
                }
            }

            int ww = wRight  - wLeft;
            int wh = wBottom - wTop;

            // Require a meaningful rectangle (at least 80×30 px)
            if (ww < 80 || wh < 30) return null;

            // Add small padding so we don't clip the edge pixels
            const int pad = 6;
            int rx = Math.Max(0,   wLeft   - pad);
            int ry = Math.Max(0,   wTop    - pad);
            int rw = Math.Min(W - rx, ww + pad * 2);
            int rh = Math.Min(H - ry, wh + pad * 2);

            AppLogger.Log($"[Detection] CAPTCHA region: ({rx},{ry}) {rw}×{rh}px");

            try { return bmp.Clone(new Rectangle(rx, ry, rw, rh), bmp.PixelFormat); }
            catch { return null; }
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
