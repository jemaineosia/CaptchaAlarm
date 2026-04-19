using System.Drawing;
using System.Drawing.Imaging;

namespace CaptchaAlarm.Services
{
    /// <summary>
    /// Captures screenshots of the primary monitor.
    /// </summary>
    public class ScreenCaptureService : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Captures the entire primary screen and returns the bitmap.
        /// Caller is responsible for disposing the returned bitmap.
        /// </summary>
        public Bitmap? CaptureScreen()
        {
            try
            {
                var bounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds
                             ?? new Rectangle(0, 0, 1920, 1080);

                var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                return bitmap;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[ScreenCaptureService] CaptureScreen failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves a bitmap to disk with a timestamped filename.
        /// </summary>
        public string? SaveScreenshot(Bitmap bitmap, string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                string filename = $"captcha_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                string fullPath = Path.Combine(directory, filename);
                bitmap.Save(fullPath, ImageFormat.Png);
                return fullPath;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[ScreenCaptureService] SaveScreenshot failed: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
