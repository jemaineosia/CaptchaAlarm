using Microsoft.Toolkit.Uwp.Notifications;

namespace CaptchaAlarm.Services
{
    /// <summary>
    /// Sends Windows 10/11 toast notifications when a CAPTCHA is detected.
    /// Falls back gracefully on older systems.
    /// </summary>
    public static class NotificationService
    {
        private static bool _registered;

        /// <summary>
        /// Registers the application as a notification source.
        /// Must be called once at application start.
        /// </summary>
        public static void Register()
        {
            if (_registered) return;
            try
            {
                // OnActivated callback requires specific WinRT TFM; skip registration
                // if the type is not available at runtime.
                RegisterActivationCallback();
                _registered = true;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Notification] Registration failed: {ex.Message}");
                _registered = true; // mark as done even if callback failed
            }
        }

        private static void RegisterActivationCallback()
        {
            // Register activation callback using the compat layer.
            // This brings the window to foreground when the toast is clicked.
            ToastNotificationManagerCompat.OnActivated += _ => OnToastActivatedInternal();
        }

        /// <summary>
        /// Shows a toast notification with the detection timestamp.
        /// </summary>
        public static void Notify(DateTime detectedAt, int confidenceScore)
        {
            try
            {
                new ToastContentBuilder()
                    .AddText("⚠️ CAPTCHA DETECTED!")
                    .AddText("Daeva Verification has appeared on your screen!")
                    .AddText($"Detected at {detectedAt.ToLocalTime():HH:mm:ss} | Score: {confidenceScore}")
                    .Show(toast =>
                    {
                        toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(2);
                    });
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Notification] Notify failed: {ex.Message}");
            }
        }

        private static void OnToastActivatedInternal()
        {
            // Bring application window to foreground when notification is clicked
            try
            {
                if (System.Windows.Forms.Application.OpenForms.Count > 0)
                {
                    var form = System.Windows.Forms.Application.OpenForms[0];
                    form?.Invoke(() =>
                    {
                        form.Show();
                        form.WindowState = System.Windows.Forms.FormWindowState.Normal;
                        form.BringToFront();
                        form.Activate();
                    });
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"[Notification] OnActivated error: {ex.Message}");
            }
        }

        /// <summary>Unregisters notification handlers on application exit.</summary>
        public static void Unregister()
        {
            try
            {
                if (_registered)
                    ToastNotificationManagerCompat.History.Clear();
            }
            catch { /* Best-effort */ }
        }
    }
}
